using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace VHistory3
{
    public class ClsSmartAssistantCalc
    {


        #region anomaly rules
        private bool checkAnomaly(SmartAssistantObject saObj)
        {
            bool madeInsert = false;
            if (saObj.DataResolution == 0)
            {
                //This means that the model haven't been trained, we just return here.
                return madeInsert;
            }
            string sql = "select Paramvalue from SmartAssistantDefExtra where Paraname='Calculation2' and SmartAssistantDefId=@SmartAssistantDefId";
            db.newCommand(sql);
            db.addParameter("@SmartAssistantDefId", saObj.SADefId);
            string calc = db.executeScalar().ToString();
            if (calc.Contains("description") || calc.Contains("calculation") || calc.Contains("io_tagname") || calc.Contains("io_tagmode") ||
              calc.Contains("channeltype") || calc.Contains("comment") || calc.Contains("unit"))
            {
                sql = "select * from histdef where (" + calc.Replace('*', '%') + ") and enabledFlag = 1";
            }
            else
            {
                sql = "select * from histdef where description like '" + calc.Replace('*', '%') + "' and enabledFlag = 1";
            }
            List<HistDefObject> histdefsreal = db.readHistDef(sql);
            List<HistDefObject> histdefprediction = new List<HistDefObject>();
            sql = "select Paramvalue from smartAssistantdefextra where smartassistantdefId=@smartassistantdefId and paramname='OutputChannels'";
            db.newCommand(sql);
            db.addParameter("@smartassistantdefId", saObj.SADefId);
            List<int> outputChannels = new List<int>();
            string outputChannelsStr = db.executeScalar().ToString();
            sql = "select * from histdef where channel=";

            string[] outputChannelsArr = outputChannelsStr.Split(';');
            foreach (string s in outputChannelsArr)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    
                    histdefprediction.AddRange(db.readHistDef(sql + s.Trim()));
                }
            }
            for (int i = 0; i < histdefsreal.Count; i++)
            {
                if (i >= histdefprediction.Count)
                {
                    //Got more channels than output channels
                    ClsLogFile.logEvent(logfilename, "In checkAnomaly(), got more channels from calculation2, than output channels, sadefId:" + saObj.SADefId);
                    break;
                }
                HistDefObject histdefreal = histdefsreal[i];
                HistDefObject histdefpred = histdefprediction[i];
                DateTime currentDate = channelsLastOkBefore[histdefpred.Channel];
                DateTime endDate = channelsLastOkAfter[histdefpred.Channel].LatestOkValueHour;
                //we look at the predicted channels lastOk as this determains when predictions have been made, and can't surpass the real.
                sql = "select value from hour where channel=@channel and logdate=@logdate";
                while (currentDate < endDate)
                {
                    db.newCommand(sql);
                    db.addParameter("@channel", histdefreal.Channel);
                    db.addParameter("@logdate", currentDate);
                    object realValObj = db.executeScalar();
                    db.newCommand(sql);
                    db.addParameter("@channel", histdefpred.Channel);
                    db.addParameter("@logdate", currentDate);
                    object predValObj = db.executeScalar();
                    if (realValObj != null && realValObj != DBNull.Value && predValObj != null && predValObj != DBNull.Value)
                    {
                        double realVal = Convert.ToDouble(realValObj);
                        double predVal = Convert.ToDouble(predValObj);
                        double distance = Math.Abs(realVal - predVal);
                        if (distance > saObj.ValueLimit)
                        {
                            madeInsert = insertAnomalyEvent(saObj, histdefreal, currentDate, realVal, predVal, "") || madeInsert;
                        }
                    }
                    else
                    {
                        ClsLogFile.logEvent(logfilename, "In checkAnomaly(), real or pred value doesn't exist, real channel: " + histdefreal.Channel + " pred channel: " + histdefpred.Channel + " for logdate: " + currentDate); 
                    }
                }
            }

            return madeInsert;
        }
        #endregion

    
    }
}