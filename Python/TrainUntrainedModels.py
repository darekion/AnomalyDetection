#from DBconnect import DBconnect
from Model import lstmNet
#from DataLoader import DataLoaderProduction, DataLoaderAnomaly, DataLoaderWebservice
from DataLoader import  DataLoaderWebservice
import torch
import numpy as np
import datetime 
import time
import xml.etree.ElementTree as ET
from urllib.request import urlopen
import json
from pathlib import Path

#Path should be updated to use virtual path, but requires absolute path for run on DTU HPC
#path = "/work3/s136587/py2/"
#path = Path().absolute()
path = ""
#Read settings from XML file.
tree = ET.parse(path + "VHistoryPython.xml")
config = tree.getroot()
serviceUrl = config.findall('serviceUrl')[0].text
checkWord = config.findall('checkWord')[0].text


use_cuda = torch.cuda.is_available()
def accuracyAnomaly(preds, targets, threshold=0.1): 
    correct_prediction = torch.zeros(preds.shape)

    for x in range(preds.shape[0]):
        for y in range(preds.shape[2]):

             if targets[x][0][y] == preds[x][0][y]:
                correct_prediction[x][0][y] = 1

             elif preds[x][0][y] > targets[x][0][y]:
                if preds[x][0][y] - (preds[x][0][y]*threshold) <= targets[x][0][y]:
                    correct_prediction[x][0][y] = 1
             elif preds[x][0][y] < targets[x][0][y]:
                if preds[x][0][y] + (preds[x][0][y]*threshold) >= targets[x][0][y]:
                    correct_prediction[x][0][y] = 1
    return torch.mean(correct_prediction.float())

def trainAnomalyLstm(x):
    saDefId = x['saDefId']
    startDate = x['StartDate']
    endDate = x['EndDate']
    print("Training model saDefId: " + str(saDefId) + " with startDate: " + startDate + " and endDate: " + endDate)  
    startDate = datetime.datetime.strptime(startDate, '%Y-%m-%dT%H:%M:%S')
    endDate= datetime.datetime.strptime(endDate, '%Y-%m-%dT%H:%M:%S')
    
    periodDays = 1
    epochs = 1000
    loader = DataLoaderWebservice(startDate,endDate,serviceUrl, saDefId, use_cuda)
    #evalEvery = 50
    #loader = DataLoaderProduction(channels, startDate, endDate, config, saDefId, use_cuda)

    trainingSplit = loader.trainingSplit 
    num_layers = 3
    lstmLr = 0.00001
    lstmDropout = 0.1
    lstmWD = 0
    hidden_size = round((loader.features_in + loader.features_out))
    firstLossCompute = True
    bestLoss = 0
    lossArr = []

    lstm = lstmNet(loader.features_in, loader.features_out, num_layers,lstmLr, lstmDropout, lstmWD, hidden_size)
    lstm.initHidden(use_cuda)

    #lstm.load(path + "state_dict sadef" + str(saDefId) + ".tar")

    if use_cuda:
        lstm.cuda()

    for x in range(epochs):
        #print("Training " + str(x))
        lstm.train()
        for i in range(loader.trainingSplit):
            #As trainingSplit might get reduced during data loading we need this check.
            if i > loader.trainingSplit:
                break
            if x == 0:
                input, target, _ = loader.getNextPeriod()
            else:
                input, target = loader.getRandomPeriod()
            

            lstmout = lstm.forward(input)
            loss = lstm.criterion(lstmout, target)
            lstm.optimizer.zero_grad()
            loss.backward()
            lstm.optimizer.step()
            lossArr.append(loss)
        loader.resetI()

        #Calculate mean loss
        sumOfLosses = 0
        for i in lossArr:
            sumOfLosses += i.item()

        meanLoss = sumOfLosses/len(lossArr)
        if firstLossCompute == True:
            bestLoss = meanLoss
            firstLossCompute = False
        else:
            if meanLoss < bestLoss:
                bestLoss = meanLoss

        lossArr = []

        
    lstm.save(path + "lstmModel saDefId" + str(saDefId) + ".tar")
    #torch.save(lstm.state_dict(), "state_dict" + str(saDefId))

f = urlopen(serviceUrl + "/Services/SmartAssistantService.ashx?type=training&CheckWord=" + str(checkWord))
modelsToTrain = json.loads(f.read())

for x in modelsToTrain:
    trainAnomalyLstm(x)
    f = urlopen(serviceUrl + "/Services/SmartAssistantService.ashx?type=trainingdone&saDefId=" + str(x['saDefId']) + "&endDate="+ x['EndDate'] + "&CheckWord=" + str(checkWord))
    f.read()
    
