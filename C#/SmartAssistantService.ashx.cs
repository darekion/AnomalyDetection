using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace VHistory3.Services
{
    /// <summary>
    /// Summary description for SmartAssistantService
    /// </summary>
    public class SmartAssistantService : IHttpHandler
    {
        ClsDb db;
       // string dateFormat;
        string logFilename = "SAService.log";
        public SmartAssistantService()
        {
            db = new ClsDb();
         //   dateFormat = "yyyy-MM-dd HH:mm:ss"; //correct dateformat for database. 

        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                if (!string.IsNullOrEmpty(context.Request.QueryString["Type"]))
                {
                    string jsonResponse = "";
                    string type = context.Request.QueryString["Type"].ToLower();
                    switch(type) 
                    {
                        case "training":
                            jsonResponse = typeTraining(context);
                            break;
                        case "trainingdone":
                            typeTrainingDone(context);
                            break;
                        case "prediction":
                            jsonResponse = typePrediction(context);
                            break;
                        case "predictiondone":
                            typePredictionDone(context);
                            break;
                        case "continuetraining":
                            jsonResponse =  typeContinueTraining(context);
                            break;
                        case "continuetrainingDone":
                            typeContinueTrainingDone(context);
                            break;
                        default:
                            ClsLogFile.logEvent(logFilename, "In SmartAssistantService.ProcessRequest(), Landed in default case with type: " + type);
                            break;
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

        private string typeTraining(HttpContext context)
        {
            List<SmartAssistantServiceDataClass> modelsToTrain = new List<SmartAssistantServiceDataClass>();
            string sql = "select SmartAssistantDef.SmartAssistantDefId, valuetype, calculation, paramvalue from SmartAssistantDef " +
                " inner join SmartAssistantDefExtra on SmartAssistantDef.SmartAssistantDefId = SmartAssistantDefExtra.SmartAssistantDefId " + 
                "where dataresolution=0 and ruletype='anomaly' and paramname='Calculation2'";
            db.newCommand(sql);
            db.executeSelect();
            List<object> result = db.readAll();
            if (result != null)
            {
                for(int i = 0; i < result.Count; i++) 
                {
                    List<object> row = result[i] as List<object>;

                    //First find an ending date - lowest LatestOkValueHour
                    DateTime inputMinDate = getLowestLatestOkValueHour(row[2].ToString());
                    DateTime targetMinDate = getLowestLatestOkValueHour(row[3].ToString());

                    DateTime endDate = targetMinDate < inputMinDate && targetMinDate != DateTime.MinValue ? targetMinDate : inputMinDate;
                    if (endDate == DateTime.MinValue)
                    {
                        ClsLogFile.logEvent(logFilename, "Enddate is minvalue - something went wrong, with sadefid:" + row[0]);
                    }
                    if (endDate > DateTime.Today.AddDays(-7).Date)
                    {
                        
                        //endDate = DateTime.Today.AddDays(-7).Date;
                        endDate = endDate.AddDays(-7);
                    }
                    SmartAssistantServiceDataClass model = new SmartAssistantServiceDataClass();
                    model.StartDate = Convert.ToDateTime(row[1]);
                    model.EndDate = endDate;
                    model.saDefId = Convert.ToInt32(row[0]);
                    modelsToTrain.Add(model);
                }
                             
            }
            return JsonConvert.SerializeObject(modelsToTrain);
        }

        private void typeTrainingDone(HttpContext context)
        {
            string saDefId = context.Request.QueryString["saDefId"];
            string sql = "update smartAssistantDef set dataresolution=1 where smartAssistantDefId=@smartAssistantDefId";
            db.newCommand(sql);
            db.addParameter("@smartAssistantDefId", saDefId);
            db.executeNonQuery();
            string endDate = context.Request.QueryString["endDate"].Replace('T',' ');
            sql = "select paramvalue from smartAssistantDefExtra where smartAssistantDefId=@smartAssistantDefId and paramname='LastTrained'";
            db.newCommand(sql);
            db.addParameter("@smartAssistantDefId", saDefId);
            object temp = db.executeScalar();
            if (temp == null || temp == DBNull.Value)
            {
                sql = "insert into smartAssistantDefExtra (smartAssistantDefId, Paramname, Paramvalue) values (@smartAssistantDefId, 'LastTrained', @endDate)";
            }
            else
            {
                sql = "update smartAssistantDefExtra set Paramvalue=@endDate where smartAssistantDefId=@smartAssistantDefId and paramname='LastTrained'";
            }
            db.newCommand(sql);
            db.addParameter("@smartAssistantDefId", saDefId);
            db.addParameter("@endDate", endDate);
            db.executeNonQuery();

            string trainingLoss = context.Request.QueryString["trainingLoss"].Replace('T', ' ');
            sql = "select paramvalue from smartAssistantDefExtra where smartAssistantDefId=@smartAssistantDefId and paramname='LastTrainingLoss'";
            db.newCommand(sql);
            db.addParameter("@smartAssistantDefId", saDefId);
            temp = db.executeScalar();
            if (temp == null || temp == DBNull.Value)
            {
                sql = "insert into smartAssistantDefExtra (smartAssistantDefId, Paramname, Paramvalue) values (@smartAssistantDefId, 'LastTrainingLoss', @trainingLoss)";
            }
            else
            {
                sql = "update smartAssistantDefExtra set Paramvalue=@trainingLoss where smartAssistantDefId=@smartAssistantDefId and paramname='LastTrainingLoss'";
            }
            db.newCommand(sql);
            db.addParameter("@smartAssistantDefId", saDefId);
            db.addParameter("@trainingLoss", trainingLoss);
            db.executeNonQuery();
        }

        private string typePrediction(HttpContext context)
        {
            List<SmartAssistantServiceDataClass> modelsToCheck = new List<SmartAssistantServiceDataClass>();
            string sql = "select SmartAssistantDef.SmartAssistantDefId, calculation, paramvalue from SmartAssistantDef " +
                " inner join SmartAssistantDefExtra on SmartAssistantDef.SmartAssistantDefId = SmartAssistantDefExtra.SmartAssistantDefId " +
                "where dataresolution=1 and ruletype='anomaly' and paramname='Calculation2'";
            db.newCommand(sql);
            db.executeSelect();
            List<object> result = db.readAll();
            if (result != null)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    List<object> row = result[i] as List<object>;

                    //First find an ending date - lowest LatestOkValueHour

                    DateTime inputMinDate = getLowestLatestOkValueHour(row[1].ToString());
                    DateTime targetMinDate = getLowestLatestOkValueHour(row[2].ToString()).AddHours(-1);
                    DateTime endDate = targetMinDate < inputMinDate && targetMinDate != DateTime.MinValue ? targetMinDate : inputMinDate;
                    if (endDate == DateTime.MinValue)
                    {
                        ClsLogFile.logEvent(logFilename, "Enddate is minvalue - something went wrong, with sadefid:" + row[0]);
                    }

                    //Get a output channel to find starting date
                    sql = "select Paramvalue from smartAssistantdefextra where smartassistantdefId=@smartassistantdefId and paramname='OutputChannels'";
                    db.newCommand(sql);
                    db.addParameter("@smartassistantdefId", row[0]);
                    List<int> outputChannels = new List<int>();
                    string outputChannelsStr = db.executeScalar().ToString();
                    string[] outputChannelsArr = outputChannelsStr.Split(';');
                    foreach (string s in outputChannelsArr)
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            outputChannels.Add(Convert.ToInt32(s.Trim()));
                        }
                    }
                    DateTime startDate = DateTime.MinValue;
                    sql = "select latestOkValueHour from histdef where channel=@channel";
                    db.newCommand(sql);
                    db.addParameter("@channel", outputChannels[0]);
                    object temp = db.executeScalar();
                    if (temp != null && temp != DBNull.Value)
                    {
                        startDate = Convert.ToDateTime(temp);
                    }
                    if (startDate == DateTime.MinValue)
                    {
                        ClsLogFile.logEvent(logFilename, "startDate is minvalue - something went wrong, with sadefid:" + row[0]);
                    }
                    if (startDate < endDate)
                    {
                        SmartAssistantServiceDataClass model = new SmartAssistantServiceDataClass();
                        model.StartDate = startDate;
                        model.EndDate = endDate;
                        model.saDefId = Convert.ToInt32(row[0]);
                        modelsToCheck.Add(model);
                    }
                    else
                    {
                        //used for debugging purposes
                        ClsLogFile.logEvent(logFilename, "No reason to check for sadefid:" + row[0] + " startdate is: " + startDate + " and enddate is: " + endDate);
                    }
                }
            }
            return JsonConvert.SerializeObject(modelsToCheck);
        }

        private void typePredictionDone(HttpContext context)
        {
            /*string saDefId = context.Request.QueryString["saDefId"];
            string endDate = context.Request.QueryString["endDate"];
            string sql = "select Paramvalue from smartAssistantdefextra where smartassistantdefId=@smartassistantdefId and paramname='OutputChannels'";
            db.newCommand(sql);
            db.addParameter("@smartassistantdefId", saDefId);
            List<int> outputChannels = new List<int>();
            string outputChannelsStr = db.executeScalar().ToString();
            string[] outputChannelsArr = outputChannelsStr.Split(';');
            foreach (string s in outputChannelsArr)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    outputChannels.Add(Convert.ToInt32(s.Trim()));
                }
            }
            sql = "update histdef set latestOkValueHour=@latestOkValueHour where channel=@channel";
            foreach (int channel in outputChannels)
            {
                db.newCommand(sql);
                db.addParameter("@latestOkValueHour", endDate);
                db.addParameter("@channel", channel);
                db.executeNonQuery();
            }*/
        }

        private string typeContinueTraining(HttpContext context)
        {
            List<SmartAssistantServiceDataClass> modelsToTrain = new List<SmartAssistantServiceDataClass>();
            string sql = "select SmartAssistantDef.SmartAssistantDefId, calculation, paramvalue from SmartAssistantDef " +
                " inner join SmartAssistantDefExtra on SmartAssistantDef.SmartAssistantDefId = SmartAssistantDefExtra.SmartAssistantDefId " +
                "where dataresolution=1 and ruletype='anomaly' and paramname='LastTrained'";
            db.newCommand(sql);
            db.executeSelect();
            List<object> result = db.readAll();
            if (result != null)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    List<object> row = result[i] as List<object>;
                    DateTime lastTrained = Convert.ToDateTime(row[2]);
                    if ((DateTime.Today - lastTrained.Date).Days < 7)
                    {
                        //we want atleast a week worth of data before we train
                        continue;
                    }

                    DateTime inputLowestDate = getLowestLatestOkValueHour(row[1].ToString());
                    if (inputLowestDate == DateTime.MaxValue || inputLowestDate < lastTrained || (inputLowestDate - lastTrained.Date).Days < 7)
                    {
                        //Need to ensure that the dates have not been pushed back
                        //we want atleast a week worth of data before we train
                        continue;
                    }

                    sql = "select paramvalue from SmartAssistantDefExtra where SmartAssistantDefId=@SmartAssistantDefId and paramname='Calculation2'";
                    db.newCommand(sql);
                    db.addParameter("@SmartAssistantDefId", row[0]);
                    object temp = db.executeScalar();
                    DateTime targetLowestDate = getLowestLatestOkValueHour(temp.ToString());
                    if (targetLowestDate == DateTime.MaxValue || targetLowestDate < lastTrained || (targetLowestDate - lastTrained.Date).Days < 7)
                    {
                        //Need to ensure that the dates have not been pushed back
                        //we want atleast a week worth of data before we train
                        continue;
                    }

                    sql = "select paramvalue from SmartAssistantDefExtra where SmartAssistantDefId=@SmartAssistantDefId and paramname='LastTrainingLoss'";
                    db.newCommand(sql);
                    db.addParameter("@SmartAssistantDefId", row[0]);
                    temp = db.executeScalar();
                    if (temp == null || temp == DBNull.Value)
                    {
                        //If loss is missing, something is wrong, don't return this model
                        ClsLogFile.logEvent(logFilename, "In typeContinueTraining(), got SADefId: " + row[0] + " with no lastTrainingLoss");
                        continue;
                    }

                    SmartAssistantServiceDataClass model = new SmartAssistantServiceDataClass();
                    model.StartDate = lastTrained;
                    model.EndDate = targetLowestDate < inputLowestDate ? targetLowestDate : inputLowestDate;
                    model.saDefId = Convert.ToInt32(row[0]);
                    model.trainingLoss = Convert.ToDouble(temp);
                    modelsToTrain.Add(model);
                }
            }

            return JsonConvert.SerializeObject(modelsToTrain);
        }

        private void typeContinueTrainingDone(HttpContext context)
        {
            //So far we can just reuse the normal trainingDone function, as we just need to update lastTrained and trainingLoss.
            //So this function is just in case we need to change it later.
            typeTrainingDone(context);
        }


            private DateTime getLowestLatestOkValueHour(string calc)
        {
            DateTime result = DateTime.MaxValue;
            calc = calc.Trim().ToLower();
            string sql;
            if (calc.Contains("description") || calc.Contains("calculation") || calc.Contains("io_tagname") || calc.Contains("io_tagmode") ||
                                   calc.Contains("channeltype") || calc.Contains("comment") || calc.Contains("unit"))
            {
                sql = "select min(LatestOkValueHour) from histdef where (" + calc.Replace('*', '%') + ") and enabledFlag = 1";
            }
            else
            {
                sql = "select min(LatestOkValueHour) from histdef where description like '" + calc.Replace('*', '%') + "' and enabledFlag = 1";
            }
            db.newCommand(sql);
            object temp = db.executeScalar();
            if (temp != null && temp != DBNull.Value)
            {
                result = Convert.ToDateTime(temp);
            }
            else
            {
                ClsLogFile.logEvent(logFilename, "Something when wrong, got no latestOkValueHour from calc:" + calc);
            }
            return result;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        private class SmartAssistantServiceDataClass
        {
            public DateTime StartDate;
            public DateTime EndDate;
            public int saDefId;
            public double trainingLoss;
        }
    }

    
}