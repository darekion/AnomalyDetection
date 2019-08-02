#from DBconnect import DBconnect
from Model import lstmNet
#from DataLoader import DataLoaderAnomaly, DataLoaderWebservice
from DataLoader import DataLoaderWebservice
import torch
import numpy as np
import datetime 
import xml.etree.ElementTree as ET
from urllib import request, parse
import json

#Path should be updated to use virtual path, but requires absolute path for run on DTU HPC
#path = "/work3/s136587/pypy2/"
path = ""

#Read settings from XML file.
tree = ET.parse(path + "VHistoryPython.xml")
config = tree.getroot()
serviceUrl = config.findall('serviceUrl')[0].text
checkWord = config.findall('checkWord')[0].text
postUrl = "/Services/CollectorService.ashx?CheckWord=" + str(checkWord) 
use_cuda = torch.cuda.is_available()

def createPredictions(x):
    saDefId = x['saDefId']
    startDate = x['StartDate']
    endDate = x['EndDate']
    print("Checking anomalies for saDefId: " + str(saDefId) + " with startDate: " + startDate + " and endDate: " + endDate)  
    startDate = datetime.datetime.strptime(startDate, '%Y-%m-%dT%H:%M:%S')
    endDate= datetime.datetime.strptime(endDate, '%Y-%m-%dT%H:%M:%S')

    for j in range(1):
        channelOffset = ((j+8) * 31)
        loader = DataLoaderWebservice(startDate,endDate,serviceUrl, saDefId, use_cuda)
        outputChannels = loader.dataLoader['outputChannels']
        trainingSplit = loader.trainingSplit 
        num_layers = 3
        lstmLr = 0.00001
        lstmDropout = 0.1
        lstmWD = 0
        hidden_size = round((loader.features_in + loader.features_out))

        accArr = []

        lstm = lstmNet(loader.features_in, loader.features_out, num_layers,lstmLr, lstmDropout, lstmWD, hidden_size)
        lstm.initHidden(use_cuda)
        lstm.load(path + "lstmModel saDefId" + str(saDefId) + ".tar", use_cuda)

        if use_cuda:
            lstm.cuda()

        lstm.eval()
        for i in range(loader.trainingSplit):

            input, target, date = loader.getNextPeriod()
            
            preds = lstm.forward(input)

            for x in range(len(outputChannels)):
                postData = []
                postData.append(outputChannels[x] + channelOffset)
                postData.append(date)
                postData.append(preds[0][0][x].item())
                #data = parse.urlencode(postData).encode()
                req =  request.Request(serviceUrl + postUrl, data=json.dumps(postData).encode('utf-8'))
                req.add_header('Content-Type', 'application/json; charset=utf-8')
                #check for sucess ? Can't get to here if we don't have access to the webserver, so should not be needed
                resp = request.urlopen(req)


#Unused for production, as title states, was used for running test locally. Uncommented to remove unused imports (DBConnect, and old dataloader)
#def localTest():
#    conn = DBconnect(config)
#
#    saDefId = 67
#    startDate = '2019-07-01'
#    endDate = '2019-07-25'
    #how it should be read.
#    for x in sys.argv:
#        if x.split('-')[0] == 'saDefId':
#            saDefId = x.split('.')[1]
#        if x.split('-')[0] == 'startDate':
#            startDate = x.split('.')[1]
#        if x.split('-')[0] == 'endDate':
#            endDate = x.split('.')[1]
#
#    startDate = datetime.datetime(int(startDate.split("-")[0]), int(startDate.split("-")[1]), int(startDate.split("-")[2]))
#    endDate = datetime.datetime(int(endDate.split("-")[0]), int(endDate.split("-")[1]), int(endDate.split("-")[2]))
#    channels = conn.getChannelList(saDefId)
#    sql = "select ValueLimit, RuleType, SAGroup from smartAssistantdef where smartAssistantdefid=" + str(saDefId)
#    df = conn.getFrame(sql)
#    sql = "select ParamName, ParamValue from smartAssistantdefExtra where smartAssistantdefid=" + str(saDefId) + " and ParamName='OutputChannels'"
#    saExtra = conn.getFrame(sql)
#    outputChannels = saExtra.ParamValue[0].split(';')
#    if outputChannels[len(outputChannels)-1] == '':
#        del outputChannels[len(outputChannels)-1]
#    loader = DataLoaderProduction(channels, startDate, endDate, config, saDefId, use_cuda)

    #Should get this from somewhere else than hardcoded.
    #features = (len(channels) + loader.datePartFeatures)
    #num_layers = 3
    #lstmLr = 0.00001
    #lstmDropout = 0.1
    #lstmWD = 0
    #hidden_size = round((features + len(channels)))
    #lstm = lstmNet(features, num_layers,lstmLr, lstmDropout, lstmWD, hidden_size, len(channels))
    #lstm.initHidden(use_cuda)
    #lstm.load("state_dict saDef" + str(saDefId) + ".tar", use_cuda)
    #lstm.eval()
    #threshold = int(df.ValueLimit[0])/100
    #predOut = []
    #targetOut = []
    #for i in range(loader.trainingSplit):
    #    #As trainingSplit might get reduced during data loading we need this check.
    #    if i > loader.trainingSplit:
    #        break
    #    input, target, channelOrder = loader.getNextPeriod(1)
    #    preds = lstm.forward(input)
    #    predOut.append(preds)
    #    targetOut.append(target)
    
        #for x in range(len(outputChannels)):
            #sql = ("insert into hour (Channel, Logdate, Value, ValueOk, Manual) values" 
            #        "(" + str(outputChannels[x]) + ", dateadd(hh, 1, '" + str(loader.lastDate) + "'),"  +str(preds[0][0][x].item()) + ",1, 0)"
            #      )
            #conn.executeNonQuery(sql, None)

        #for x in range(target.shape[0]):
            #for y in range(target.shape[2]):
                 #if targets[x][0][y] == lstmout[x][0][y]:
                #    #correct_prediction[x][0][y] = 1
                #insert = False
                #if preds[x][0][y] > target[x][0][y]:
                #    if preds[x][0][y] - (preds[x][0][y]*threshold) > target[x][0][y]:
                #        insert = True
                #elif preds[x][0][y] < target[x][0][y]:
                #    if preds[x][0][y] + (preds[x][0][y]*threshold) < target[x][0][y]:
                #        insert = True

                #insert = False
                #if insert:
                    #find starting hour, used when sequence length was 1 day
                    #for j in range(23):
                    #    if input[x][0][j+7] == 1:
                    #        hourIndex = j
                    #        break

                    #Insert new occurence in SA table for anomaly, might need to be done by a webservice later.
                    #sql = ("insert into smartassistant (Channel, OwnerName, TimeOccured, Value, ValueLimit, OccSpecific, OccType, RuleType, SAGroup, Acknowledged, Comment, smartAssistantDefId) values" 
                    #       "(" + str(channelOrder[y]) + ",'', '" + str(loader.lastDate) + "'), "  +str(preds[x][0][y].item()) + "," + str(target[x][0][y].item()) + ","
                    #       "0,'','" + str(df.RuleType[0]) + "','" + str(df.SAGroup[0]) + "',0,'', " + str(saDefId) + ")"
                    #       )
                    #conn.executeNonQuery(sql, None)
        #lstmacc = accuracyAnomaly(lstmout, target)
        #analyse accs 
    

    #Write to CSV files - used for testing
    #writeOut1 = []
    #writeOut2 = []
    #for x in range(len(predOut)):
    #    tarArr = []
    #    predArr = []
    #    for y in range(predOut[x].shape[2]):
    #        tarArr.append(targetOut[x][0][0][y].item())
    #        predArr.append(predOut[x][0][0][y].item())
    #
    #    writeOut1.append(predArr)
    #    writeOut2.append(tarArr)
    #np.savetxt("Preds.csv", writeOut1, delimiter=",")
    #np.savetxt("Targets.csv", writeOut2, delimiter=",")



#Get models to do prediction for, from V-History
f = request.urlopen(serviceUrl + "/Services/SmartAssistantService.ashx?type=prediction&CheckWord=" + str(checkWord))
modelsToPredict = json.loads(f.read())

#Do predictions for all (if any) models
for x in modelsToPredict:
    createPredictions(x)
    saDefId = x['saDefId']
    endDate = x['EndDate']
    f = request.urlopen(serviceUrl + "/Services/SmartAssistantService.ashx?type=predictiondone&saDefId=" + str(x['saDefId']) + "&endDate=" + endDate + "&CheckWord=" + str(checkWord))
    #f.read()
    