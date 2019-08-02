from Model import lstmNet
from DataLoader import  DataLoaderWebservice
import torch
import numpy as np
import datetime 
import time
import xml.etree.ElementTree as ET
from urllib.request import urlopen
import json
from pathlib import Path

def continueTraining(x):
    saDefId = x['saDefId']
    startDate = x['StartDate']
    endDate = x['EndDate']
    startDate = datetime.datetime.strptime(startDate, '%Y-%m-%dT%H:%M:%S')
    endDate= datetime.datetime.strptime(endDate, '%Y-%m-%dT%H:%M:%S')
    
    periodDays = 1
    epochs = 1000
    loader = DataLoaderWebservice(startDate,endDate,serviceUrl, saDefId, use_cuda)

    trainingSplit = loader.trainingSplit 
    num_layers = 3
    lstmLr = 0.00001
    lstmDropout = 0.1
    lstmWD = 0
    hidden_size = round((loader.features_in + loader.features_out))

    accArr = []

    lstm = lstmNet(loader.features_in, loader.features_out, num_layers,lstmLr, lstmDropout, lstmWD, hidden_size)
    lstm.initHidden(use_cuda)

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
            lstmloss = lstm.criterion(lstmout, target)
            lstmacc = accuracyAnomaly(lstmout, target)
            lstm.optimizer.zero_grad()
            lstmloss.backward()
            lstm.optimizer.step()
            accArr.append(lstmacc)

        loader.resetI()

        if (x+1) % 10 == 0:
            computedAcc = 0
            for i in accArr:
                computedAcc += i.item()

            meanAcc = computedAcc/len(accArr)
            #if  meanAcc > 0.25:
            if (x+1) % 100 == 0:
                lstm.save(path + "state_dict saDef " + str(saDefId) + " epoch " + str(x) + " acc " + str(meanAcc) + ".tar")
            
            print("Epoch: " + str(x) +" Mean acc: " + str(meanAcc))

            accArr = []

        
    lstm.save(path + "state_dict saDef" + str(saDefId) + " final.tar")

    #lstm.load(path + "state_dict sadef" + str(saDefId) + ".tar")
#path = "/work3/s136587/py2/"
path = Path().absolute()
path = ""
tree = ET.parse(path + "VHistoryPython.xml")
config = tree.getroot()
serviceUrl = config.findall('serviceUrl')[0].text
f = urlopen(serviceUrl + "/Services/SmartAssistantService.ashx?type=training")
modelsToTrain = json.loads(f.read())

for x in continueTraining:
    trainAnomalyLstm(x)
    f = urlopen(serviceUrl + "/Services/SmartAssistantService.ashx?type=trainingdone&saDefId=" + str(x['saDefId']) + "&endDate="+ x['EndDate'])
    f.read()