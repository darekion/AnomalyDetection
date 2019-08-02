from Model import lstmNet, lrModel
from DataLoader import DataLoaderPredictionSingleChannel, DataLoaderAnomaly
import torch
import numpy as np
import datetime 
import time
import sys

use_cuda = torch.cuda.is_available()
print("use cuda: " + str(use_cuda))
#Full channel set for Fruedal waterplant
channels = [400, 401,402,403,410,411,412,413,414,420,445,500,501,502,503,504,505,506,520,
            521,522,523,524,525,526,540,541,542,543,544,545,546]

startPeriod = datetime.date(2017,1,1)
endPeriod = datetime.date(2018,1,1)
features = len(channels) + 31 #channels + hour + weekday


def get_variable(x):
    """ Converts tensors to cuda, if available. """
    if use_cuda:
        return x.cuda()
    return x

def get_numpy(x):
    """ Get numpy array for both cuda and not. """
    if use_cuda:
        return x.cpu().data.numpy()
    return x.data.numpy()

def accuracyPrediction(preds, targets, threshold=0.1):
    # making a one-hot encoded vector of correct (1) and incorrect (0) predictions
    correct_prediction = torch.zeros(preds.shape)
    for x in range(preds.shape[0]):
        if targets[x] == preds[x]:
            correct_prediction[x] = 1
        elif preds[x] > targets[x]:
            if preds[x] - (preds[x]*threshold) <= targets[x]:
                correct_prediction[x] = 1
        elif preds[x] < targets[x]:
            if preds[x] + (preds[x]*threshold) >= targets[x]:
                correct_prediction[x] = 1
    #correct_prediction = torch.eq(ys, ts)
    # averaging the one-hot encoded vector
    return torch.mean(correct_prediction.float())

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

def saveToCsv(name, array, ):
    filename = name + ".csv"
    np.savetxt(filename, array, delimiter=",")

def trainPredictionModels():
    #lstm.load_state_dict(torch.load("state_dict"))

    trainingSplit = 250
    evalSplit = 50
    periodDays = 1
    epochs = 1000
    evalEvery = 50
    loader = DataLoaderPredictionSingleChannel(channels, 440, startPeriod, endPeriod)

    for y in range(2):
        lstmLosses = []
        lrLosses = []
        lstmAcces = []
        lrAcces = []
        lstmLossesOut = []
        lrLossesOut = []
        lstmLossesEvalOut = []
        lrLossesEvalOut = []
        lstmAccesEvalOut = []
        lrAccesEvalOut = []
        start = time.time()
        if y == 0:
            num_layers = 3
            lstmLr = 0.00001
            lstmDropout = 0.1
            lstmWD = 0.1
            hidden_size = round(  (features +1)* 2 /3) 

            linearLr = 0.000001
            linearWD = 0.15
        elif y == 1:
            num_layers = 3
            lstmLr = 0.00001
            lstmDropout = 0.1
            lstmWD = 0
            hidden_size = round((features +1))

            linearLr = 0.000001
            linearWD = 0.2
        

        lstm = lstmNet(features,num_layers,lstmLr, lstmDropout, lstmWD, hidden_size, 1)
        lstm.initHidden(use_cuda)
        lr = lrModel(features,linearLr,linearWD)
        if use_cuda:
            lstm.cuda()
            lr.cuda()
        for x in range(epochs):
            #print("Training " + str(x))
            lstm.train()
            lr.train()
    
            for i in range(trainingSplit):
                if x == 0:
                    input, target = loader.getNextPeriod(periodDays)
                else:
                    input, target = loader.getRandomPeriod()

                input = get_variable(input)
                target = get_variable(target)

                #lstm.zero_grad()
                lstmout = lstm.forward(input)
                lstmloss = lstm.criterion(lstmout,target)
                lstmacc = accuracyPrediction(lstmout, target)
                lstm.optimizer.zero_grad()
                lstmloss.backward()
                lstm.optimizer.step()
                lstmLosses.append(lstmloss)
                lstmLossesOut.append(lstmloss)
                #lstmAccesOut.append(lstmacc)

                out = lr.forward(input)
                loss = lr.criterion(out,target)
                acc = accuracyPrediction(out, target)
                lr.optimizer.zero_grad()
                loss.backward()
                lr.optimizer.step()
                lrLosses.append(loss)
                #lrLossesOut.append(loss)

                #lrAccesOut.append(acc)
                #print("lr acc:" + str(round(acc.item(),2)) +  " lr loss: " + str(round(loss.item(),2)) + " lsmt acc: " + str(round(lstmacc.item(),2)) + " lsmt loss: " + str(round(lstmloss.item(),2)))
    
            loader.resetI(trainingSplit)

            #if (x+1) % 1 == 0:
            #    computedLoss = 0
            #    computedLossLr = 0
            #    for i in lstmLosses:
            #        computedLoss += i.item()
            #    for i in lrLosses:
            #        computedLossLr += i.item()
            #    print("Epoch: " + str(x) +" Lstm mean loss: " + str(computedLoss/(trainingSplit*10)) + " Lr mean loss: " + str(computedLossLr / (trainingSplit*10)))
            #    lstmLosses = []
            #    lrLosses = []

            if (x+1) % evalEvery == 0:
                print("Evaluating "+ str(x) )
                lstm.eval()
                lr.eval()
                for i in range(evalSplit):
                    if x== (evalEvery -1):
                        input, target = loader.getNextPeriod(periodDays)
                    else: 
                        input, target = loader.getPeriodI(i+trainingSplit)

                    input = get_variable(input)
                    target = get_variable(target)

                    lstmout = lstm.forward(input)
                    lstmloss = lstm.criterion(lstmout,target)
                    lstmacc = accuracyPrediction(lstmout, target)
                    lstmLosses.append(lstmloss)
                    lstmLossesEvalOut.append(lstmloss)
                    lstmAccesEvalOut.append(lstmacc)

                    out = lr.forward(input)
                    loss = lr.criterion(out,target)
                    acc = accuracyPrediction(out, target)
                    lrLosses.append(loss)
                    lrLossesEvalOut.append(loss)
                    lrAccesEvalOut.append(acc)

                    #print("lr acc:" + str(round(acc.item(),2)) +  " lr loss: " + str(round(loss.item(),2)) + " lsmt acc: " + str(round(lstmacc.item(),2)) + " lsmt loss: " + str(round(lstmloss.item(),2)))

                computedLoss = 0
                computedLossLr = 0
                for i in lstmLosses:
                    computedLoss += i.item()
                for i in lrLosses:
                    computedLossLr += i.item()
                print("Epoch: " + str(x) +"Evaluation Lstm mean loss: " + str(computedLoss/evalSplit) + " Lr mean loss: " + str(computedLossLr / evalSplit))
                lstmLosses = []
                lrLosses = []

        end = time.time()
        print("Run " + str(y) + " took: " + str(end - start) + "ms")
        saveToCsv("/work3/s136587/results/lstmLosses" + str(y), lstmLossesOut)
        saveToCsv("/work3/s136587/results/lrLosses" + str(y), lrLossesOut)
        saveToCsv("/work3/s136587/results/lstmLossesEval" + str(y), lstmLossesEvalOut)
        saveToCsv("/work3/s136587/results/lrLossesEval" + str(y), lrLossesEvalOut)
        saveToCsv("/work3/s136587/results/lstmAccesEval" + str(y), lstmAccesEvalOut)
        saveToCsv("/work3/s136587/results/lrAccesEval" + str(y), lrAccesEvalOut)
        torch.save(lstm.state_dict(), "/work3/s136587/results/lstm state_dict" + str(y))
        torch.save(lr.state_dict(), "/work3/s136587/results/linear state_dict" + str(y))
        #save results in csv

def trainAnomalyLstm():
    trainingSplit = 250
    evalSplit = 50
    periodDays = 1
    epochs = 1000
    evalEvery = 50
    loader = DataLoaderAnomaly(channels, startPeriod, endPeriod)

    for y in range(1):
        if y == 0:
            num_layers = 10
            lstmLr = 0.00001
            lstmDropout = 0.1
            lstmWD = 0
            hidden_size = round((features + len(channels))/1.4)
        elif y == 1:
            num_layers = 2
            lstmLr = 0.00001
            lstmDropout = 0.1
            lstmWD = 0
            hidden_size = round((features + len(channels))*1.4)

        elif y == 2:
            num_layers = 10
            lstmLr = 0.00001
            lstmDropout = 0.2
            lstmWD = 0
            hidden_size = round((features + len(channels))*1.4)


        lstmLossesEvalOut = []
        lstmAccesEvalOut = []
        lstmLossesOut = []
        lstmLosses = []


        lstm = lstmNet(features,num_layers,lstmLr, lstmDropout, lstmWD, hidden_size, len(channels))
        lstm.initHidden(use_cuda)
        #lstm.load_state_dict(torch.load("/work3/s136587/pyAnom/lstm state_dict"))
        #lstm.load_state_dict(torch.load("lstm state_dict"))
        if use_cuda:
            lstm.cuda()

        for x in range(epochs):
            #print("Training " + str(x))
            lstm.train()
            for i in range(trainingSplit):
                if x == 0:
                    input, target = loader.getNextPeriod(periodDays)
                else:
                    input, target = loader.getRandomPeriod()
            
                input = get_variable(input)
                target = get_variable(target)


                lstmout = lstm.forward(input)
                lstmloss = lstm.criterion(lstmout, target)
                lstmacc = accuracyAnomaly(lstmout, target)
                lstm.optimizer.zero_grad()
                lstmloss.backward()
                lstm.optimizer.step()
                lstmLosses.append(lstmloss)
                #lstmLossesOut.append(lstmloss)

            loader.resetI(trainingSplit)

            if (x+1) % 1 == 0:
                computedLoss = 0
                for i in lstmLosses:
                    computedLoss += i.item()

                #print("Epoch: " + str(x) +" Lstm mean loss: " + str(computedLoss/trainingSplit))
                lstmLosses = []

            if (x+1) % evalEvery == 0:
                #print("Evaluating "+ str(x) )
                lstm.eval()

                for i in range(evalSplit):
                    if x== (evalEvery -1):
                        input, target = loader.getNextPeriod(periodDays)
                    else: 
                        input, target = loader.getPeriodI(i+trainingSplit)

                    input = get_variable(input)
                    target = get_variable(target)

                    lstmloss = lstm.criterion(lstmout, target)
                    lstmacc = accuracyAnomaly(lstmout, target)
                    lstmLossesEvalOut.append(lstmloss)
                    lstmAccesEvalOut.append(lstmacc)
                    lstmLosses.append(lstmloss)

                computedLoss = 0
                for i in lstmLosses:
                    computedLoss += i.item()

                #print("Epoch: " + str(x) +" Lstm eval mean loss: " + str(computedLoss/evalSplit))
                lstmLosses = []

        #saveToCsv("/work3/s136587/resultsAnom/lstmLosses" + str(y), lstmLossesOut)
        saveToCsv("/work3/s136587/resultsAnom/lstmLossesEval" + str(y), lstmLossesEvalOut)
        saveToCsv("/work3/s136587/resultsAnom/lstmAccesEval" + str(y), lstmAccesEvalOut)
        torch.save(lstm.state_dict(), "/work3/s136587/resultsAnom/lstm state_dict" + str(y))
#trainPredictionModels()
trainAnomalyLstm()