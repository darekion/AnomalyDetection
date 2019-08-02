using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace VHistory3
{
    /// <summary>
    /// Summary description for DataByChannel
    /// </summary>
    public class DataService : IHttpHandler
    {
        ClsDb db;
        string dateFormat;
        string logFilename = "DataService.log";
        string dateAddStr;
        int offset;
        public DataService()
        {
            db = new ClsDb();
            dateFormat = "yyyy-MM-dd HH:mm:ss"; //having the dateformat set one place, 

        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {

                //because we sometimes need single data, the type parameter will determain how to procced
                //different types are: Graph, which returns dates with values and settings for graphs
                //                      GEUS, which returns the date of the lastest value based on the channel
                //                  countervalue, which returns the lastest value from the channel
                if (!string.IsNullOrEmpty(context.Request.QueryString["Type"]))
                {
                    string jsonResponse = "";
                    if (context.Request.QueryString["Type"].ToLower() == "graph")
                    {
                        jsonResponse = typeGraph(context);
                    }
                    else if (context.Request.QueryString["Type"].ToLower() == "countervalue")
                    {
                        //This must be changed once values start getting moved into the hour table, where will the last value be put ?
                        string sql = "select top 1 Value from RawData where Channel = @Channel order by LogDate desc";
                        db.newCommand(sql);
                        db.addParameter("@Channel", context.Request.QueryString["Channel"]);
                        jsonResponse = db.executeScalar().ToString();
                    }
                    else if (context.Request.QueryString["Type"].ToLower() == "geus")
                    {
                        //we get the newest data, VHOpdat will make sure data are put in sporadic correctly
                        string sql = "select top 1 LogDate from RawData where Channel = @Channel order by LogDate desc";
                        db.newCommand(sql);
                        db.addParameter("@Channel", context.Request.QueryString["Channel"]);
                        jsonResponse = db.executeScalar().ToString();
                    }
                    else if (context.Request.QueryString["Type"].ToLower() == "anomaly")
                    {
                        jsonResponse = typeAnomaly(context);
                    }
                    context.Response.ContentType = "application/json";
                    context.Response.Output.Write(jsonResponse);
                }
                else
                {
                    //unknown type, should return some error
                }
            }
            catch (Exception ex)
            {
                //context.Response.Output.Write("Error occoured: " + ex.Message);
                ClsLogFile.logEvent(logFilename, "In ProcessRequest(), Message: " + ex.Message + " Stacktrace: " + ex.StackTrace);
            }
            finally
            {
                context.Response.Flush(); // Sends all currently buffered output to the client.
                context.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                context.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
                if (db != null)
                {
                    db.closeConnection(); // we close the mssql database connection again. 
                }
            }
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        private bool checkIfChannelIsNoHourData(HistDefObject histdef)
        {
            bool result = false;
            if (histdef.Calculation.StartsWith("time") || histdef.Calculation.StartsWith("daycal") || histdef.Calculation.StartsWith("leak") || 
                histdef.Calculation.StartsWith("tell") || histdef.Calculation.StartsWith("grad")) 
            {
                result = true;
            }
            return result;
        }

        #region methods for type = anomaly
        private string typeAnomaly(HttpContext context)
        {
            string saDefId = context.Request.QueryString["SADefId"].ToString();
            DateTime startDate = Convert.ToDateTime(context.Request.QueryString["StartDate"].Replace('T',' '));
            DateTime endDate = Convert.ToDateTime(context.Request.QueryString["EndDate"].Replace('T', ' '));

            //Get calculation and calculation2 to select channel lists
            string sql = "select Calculation, ParamValue from SmartAssistantDef inner join SmartAssistantDefExtra on " +
                "SmartAssistantDef.SmartAssistantDefId=SmartAssistantDefExtra.SmartAssistantDefId " +
                " where SmartAssistantDef.SmartAssistantDefId=@SmartAssistantDefId and ParamName='Calculation2'";
            db.newCommand(sql);
            db.addParameter("@SmartAssistantDefId", saDefId);
            db.executeSelect();
            List<object> calcs = db.readAll()[0] as List<object>;
            //Get channels for input
            string calc = calcs[0].ToString().Trim().ToLower();
            if (calc.Contains("description") || calc.Contains("calculation") || calc.Contains("io_tagname") || calc.Contains("io_tagmode") ||
               calc.Contains("channeltype") || calc.Contains("comment") || calc.Contains("unit"))
            {
                sql = "select channel from histdef where (" + calc.Replace('*', '%') + ") and enabledFlag = 1";
            }
            else
            {
                sql = "select channel from histdef where description like '" + calc.Replace('*', '%') + "' and enabledFlag = 1";
            }
            List<HistDefObject> inputChannels = db.readHistDef(sql);

            //Get channels for targets
            calc = calcs[1].ToString().Trim().ToLower();
            if (calc.Contains("description") || calc.Contains("calculation") || calc.Contains("io_tagname") || calc.Contains("io_tagmode") ||
               calc.Contains("channeltype") || calc.Contains("comment") || calc.Contains("unit"))
            {
                sql = "select channel from histdef where (" + calc.Replace('*', '%') + ") and enabledFlag = 1";
            }
            else
            {
                sql = "select channel from histdef where description like '" + calc.Replace('*', '%') + "' and enabledFlag = 1";
            }
            List<HistDefObject> targetChannels = db.readHistDef(sql);

            List<DateTime> incompleteAndExcludedDates = new List<DateTime>();
            //Add excluded dates to the list before we start adding data so we don't have to filter the excluded data out later.
            sql = "select StartDate, EndDate from SAExcludedDates where SmartAssistantDefId=@SmartAssistantDefId";
            db.newCommand(sql);
            db.addParameter("@SmartAssistantDefId", saDefId);
            db.executeSelect();
            List<object> excludedDates = db.readAll();
            if (excludedDates != null)
            {
                for (int i = 0; i < excludedDates.Count; i++)
                {
                    List<object> row = excludedDates[i] as List<object>;
                    DateTime startOfInterval = Convert.ToDateTime(row[0]);
                    DateTime endOfInterval = Convert.ToDateTime(row[1]);
                    DateTime currDateTime = startOfInterval;
                    while (currDateTime < endOfInterval)
                    {
                        incompleteAndExcludedDates.Add(currDateTime);
                        currDateTime = currDateTime.AddHours(1); //Need to get updated to minutes when support for higher data resolution is added.
                    }
                }
            }


            AnomalyData anomalyData = new AnomalyData();
            int inputFeatures = inputChannels.Count + 32; //31 being datepart features, should be 32 once holiday is implemented
            sql = getSqlSelectForAnomaly(inputChannels);
            db.newCommand(sql);
            db.addParameter("@startDate", startDate.ToString(dateFormat));
            db.addParameter("@endDate", endDate.ToString(dateFormat));
            db.executeSelect();
            int inputCursor = -1;
            List<object> inputs = db.readAll();
            DateTime currDate = DateTime.MinValue;
            for (int i = 0; i < inputs.Count; i++)
            {
                List<object> row = inputs[i] as List<object>;
                bool onlyValue = true;
                DateTime rowDate = Convert.ToDateTime(row[2]);
                if (incompleteAndExcludedDates.Contains(rowDate))
                {
                    continue;
                }
                if (rowDate != currDate)
                {
                    if (anomalyData.inputs.Count > 0 && anomalyData.inputs[inputCursor].Count < inputFeatures)
                    {
                        //This means some channels have data missing, we exclude this datetime then.
                        anomalyData.inputs[inputCursor] = new List<double>();
                        incompleteAndExcludedDates.Add(currDate);
                    }
                    else
                    {
                        anomalyData.inputs.Add(new List<double>());
                        inputCursor = anomalyData.inputs.Count - 1;
                    }
                    onlyValue = false;
                    currDate = rowDate;
                }
                
                if (!onlyValue)
                {
                    for (int j = 3; j < row.Count; j++)
                    {
                        anomalyData.inputs[inputCursor].Add(Convert.ToDouble(row[j]));
                    }
                    
                    anomalyData.inputs[inputCursor].Add(Convert.ToDouble(Nager.Date.DateSystem.IsPublicHoliday(rowDate, Nager.Date.CountryCode.DK)));
                }
                anomalyData.inputs[inputCursor].Add(Convert.ToDouble(row[0]));
            }
            if (anomalyData.inputs[inputCursor].Count < inputFeatures)
            {
                //This means some channels have data missing, we exclude this datetime then.
                if (anomalyData.inputs.Count > inputCursor)
                {
                    anomalyData.inputs.RemoveAt(inputCursor);
                    incompleteAndExcludedDates.Add(currDate);
                    anomalyData.dates.Remove(currDate);
                }
            }


            sql = getSqlSelectForAnomaly(targetChannels);
            db.newCommand(sql);
            startDate = startDate.AddHours(1);
            endDate = endDate.AddHours(1);
            db.addParameter("@startDate", startDate.ToString(dateFormat));
            db.addParameter("@endDate", endDate.ToString(dateFormat));
            db.executeSelect();
            List<object> targets = db.readAll();
            int targetCursor = -1;
            int targetFeatures = targetChannels.Count;
            currDate = DateTime.MinValue;
            for (int i = 0; i < targets.Count; i++)
            {
                List<object> row = targets[i] as List<object>;
                DateTime rowDate = Convert.ToDateTime(row[2]);
                if (incompleteAndExcludedDates.Contains(rowDate.AddHours(-1)))
                {
                    continue;
                }
                if (rowDate != currDate)
                {
                    if (anomalyData.targets.Count > 0 && anomalyData.targets[targetCursor].Count < targetFeatures)
                    {
                        anomalyData.targets[targetCursor] = new List<double>();
                        incompleteAndExcludedDates.Add(currDate.AddHours(-1));
                        if (anomalyData.inputs.Count > targetCursor)
                        {
                            anomalyData.inputs.RemoveAt(targetCursor);
                        }
                        if (anomalyData.dates.Count > targetCursor)
                        {
                            anomalyData.dates[targetCursor] = rowDate.AddHours(-1);
                        }
                    }
                    else
                    {
                        anomalyData.targets.Add(new List<double>());
                        targetCursor = anomalyData.targets.Count - 1;
                        anomalyData.dates.Add(rowDate.AddHours(-1));
                    }
                    currDate = rowDate;
                }
                
                anomalyData.targets[targetCursor].Add(Convert.ToDouble(row[0]));
            }
            if (anomalyData.targets[targetCursor].Count < targetFeatures)
            {
                if (anomalyData.inputs.Count > targetCursor)
                {
                    anomalyData.inputs.RemoveAt(targetCursor);
                }
                if (anomalyData.targets.Count > targetCursor)
                {
                    anomalyData.targets.RemoveAt(targetCursor);
                }
                if (anomalyData.dates.Count > targetCursor)
                {
                    anomalyData.dates.RemoveAt(targetCursor);
                }
                
            }

            sql = "select Paramvalue from smartAssistantdefextra where smartassistantdefId=@smartassistantdefId and paramname='OutputChannels'";
            db.newCommand(sql);
            db.addParameter("@smartassistantdefId", saDefId);
            
            string outputChannelsStr = db.executeScalar().ToString();
            string[] outputChannelsArr = outputChannelsStr.Split(';');
            foreach (string s in outputChannelsArr)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    anomalyData.outputChannels.Add(Convert.ToInt32(s.Trim()));
                }
            }

            return JsonConvert.SerializeObject(anomalyData);
        }

        private string getSqlSelectForAnomaly(List<HistDefObject> channels)
        {
            string sql = "select value, channel, logdate, " +
                  "(case when datepart(dw,logdate) = 7 then 1 else 0 end) as weekday0, " +
	              "(case when datepart(dw,logdate) = 1 then 1 else 0 end) as weekday1, " +
	              "(case when datepart(dw,logdate) = 2 then 1 else 0 end) as weekday2, " +
	              "(case when datepart(dw,logdate) = 3 then 1 else 0 end) as weekday3, " +
	              "(case when datepart(dw,logdate) = 4 then 1 else 0 end) as weekday4, " +
	              "(case when datepart(dw,logdate) = 5 then 1 else 0 end) as weekday5, " +
	              "(case when datepart(dw,logdate) = 6 then 1 else 0 end) as weekday6, " +
	              "(case when datepart(hh,logdate) = 0 then 1 else 0 end) as hh0, " +
	              "(case when datepart(hh,logdate) = 1 then 1 else 0 end) as hh1, " +
	              "(case when datepart(hh,logdate) = 2 then 1 else 0 end) as hh2, " +
	              "(case when datepart(hh,logdate) = 3 then 1 else 0 end) as hh3, " +
	              "(case when datepart(hh,logdate) = 4 then 1 else 0 end) as hh4, " +
	              "(case when datepart(hh,logdate) = 5 then 1 else 0 end) as hh5, " +
	              "(case when datepart(hh,logdate) = 6 then 1 else 0 end) as hh6, " +
	              "(case when datepart(hh,logdate) = 7 then 1 else 0 end) as hh7, " +
	              "(case when datepart(hh,logdate) = 8 then 1 else 0 end) as hh8, " +
	              "(case when datepart(hh,logdate) = 9 then 1 else 0 end) as hh9, " +
	              "(case when datepart(hh,logdate) = 10 then 1 else 0 end) as hh10, " +
	              "(case when datepart(hh,logdate) = 11 then 1 else 0 end) as hh11, " +
	              "(case when datepart(hh,logdate) = 12 then 1 else 0 end) as hh12, " +
	              "(case when datepart(hh,logdate) = 13 then 1 else 0 end) as hh13, " +
	              "(case when datepart(hh,logdate) = 14 then 1 else 0 end) as hh14, " +
	              "(case when datepart(hh,logdate) = 15 then 1 else 0 end) as hh15, " +
	              "(case when datepart(hh,logdate) = 16 then 1 else 0 end) as hh16, " +
	              "(case when datepart(hh,logdate) = 17 then 1 else 0 end) as hh17, " +
	              "(case when datepart(hh,logdate) = 18 then 1 else 0 end) as hh18, " +
	              "(case when datepart(hh,logdate) = 19 then 1 else 0 end) as hh19, " +
	              "(case when datepart(hh,logdate) = 20 then 1 else 0 end) as hh20, " +
	              "(case when datepart(hh,logdate) = 21 then 1 else 0 end) as hh21, " +
	              "(case when datepart(hh,logdate) = 22 then 1 else 0 end) as hh22, " + 
	              "(case when datepart(hh,logdate) = 23 then 1 else 0 end) as hh23 " +
                  " from hour where ";
            for (int i = 0; i < channels.Count; i++)
            {
                HistDefObject hist = channels[i];
                if (i == 0)
                {
                    sql += "(channel=" + hist.Channel;
                }
                else
                {
                    sql += " or channel=" + hist.Channel;
                }
                
            }
            sql += ") and logdate>=@startDate and logdate <=@endDate order by logdate asc, channel asc";
            return sql;
        }
        #endregion
    }
}