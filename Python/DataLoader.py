#from DBconnect import DBconnect
import numpy as np
import datetime 
#from datetime import timedelta
import calendar
import torch
from urllib.request import urlopen
import json

#class DataLoaderPredictionSingleChannel():
#    def __init__(self, channels, predictChan, startDate, endDate):
#        db = DBconnect(None)
#        self.db = db
#        self.channels = channels
#        self.startDate = startDate
#        self.endDate = endDate
#        self.currentDate = startDate
#        self.predictChan = predictChan
#        self.channelsSql = db.createChannelsSql(channels, predictChan)
#        self.data = []
#        #self.datePartFeatures = 2
#        self.datePartFeatures = 31


#    def getNextPeriod(self, days):
#        channels = self.channels
#        #need to get longest sequence
#        nextDate = self.add_day(self.currentDate, days)
#        #nextDate = self.add_months(self.currentDate, 1)
#
#        #nextDate = self.add_months(self.currentDate, 1)
#        hours = (nextDate - self.currentDate).days * 24
#        input = np.empty([hours,1,len(channels)+self.datePartFeatures])
#        target = np.empty([hours,1,1])
#        length = len(channels)
#        #Loop where we select all channels pr datetime
#        for x in range(hours):
#            logdateWhere = "logdate=dateadd(hh, " + str(x) + ",'" + str(self.currentDate) + "')"
#            df = self.db.getFrame(self.channelsSql + " and " + logdateWhere)
#            #dfTarget = self.db.getFrame("select datepart(hh, logdate) as hour, datepart(dw, logdate) as weekday, value, channel from hour where channel="+str(self.predictChan) + " and " + logdateWhere)
#            channels = self.channels.copy()
#            maxC = len(channels)
#            for i, row in df.iterrows():
#                chanOffset = maxC - len(channels)
#                for c in range(len(channels)):
#                    if row.channel == channels[c]:
#                        if i == 0: 
#                            input[x,0,0] = row.weekday0
#                            input[x,0,1] = row.weekday1
#                            input[x,0,2] = row.weekday2
#                            input[x,0,3] = row.weekday3
#                            input[x,0,4] = row.weekday4
#                            input[x,0,5] = row.weekday5
#                            input[x,0,6] = row.weekday6
#                            input[x,0,7] = row.hh0
#                            input[x,0,8] = row.hh1
#                            input[x,0,9] = row.hh2
#                            input[x,0,10] = row.hh3
#                            input[x,0,11] = row.hh4
#                            input[x,0,12] = row.hh5
#                            input[x,0,13] = row.hh6
#                            input[x,0,14] = row.hh7
#                            input[x,0,15] = row.hh8
#                            input[x,0,16] = row.hh9
#                            input[x,0,17] = row.hh10
#                            input[x,0,18] = row.hh11
#                            input[x,0,19] = row.hh12
#                            input[x,0,20] = row.hh13
#                            input[x,0,21] = row.hh14
#                            input[x,0,22] = row.hh15
#                            input[x,0,23] = row.hh16
#                            input[x,0,24] = row.hh17
#                            input[x,0,25] = row.hh18
#                            input[x,0,26] = row.hh19
#                            input[x,0,27] = row.hh20
#                            input[x,0,28] = row.hh21
#                            input[x,0,29] = row.hh22
#                            input[x,0,30] = row.hh23
#                            input[x,0,(c+self.datePartFeatures + chanOffset)] = row.value
#                            del channels[c]
#                            break;
#                        else:
#                            input[x,0,(c+self.datePartFeatures + chanOffset)] = row.value
#                            del channels[c]
#                           break;
#                   elif row.channel == self.predictChan:
#                       target[x,0,0] = row.value
#                      break;
#          
#        self.currentDate = nextDate
#        input = torch.from_numpy(input).float()
#        target = torch.from_numpy(target).float()
#        self.data.append([input,target])
#        return input, target

#    def getPeriodI(self, i):
#        return self.data[i]

#    def getRandomPeriod(self):
#        outI = np.random.choice(self.iArr, size=1, replace=False, p=None)
#        self.iArr.remove(outI)

#       return self.getPeriodI(outI[0])

#    def resetI(self, trainingSplit):
#        self.iArr = []
#        for i in range (trainingSplit):
#            self.iArr.append(i)

#   def add_day(self, sourcedate, days):
#        return sourcedate + datetime.timedelta(days)

#    def add_months(self, sourcedate,months):
#        month = sourcedate.month - 1 + months
#        year = sourcedate.year + month // 12
#        month = month % 12 + 1
#        day = min(sourcedate.day,calendar.monthrange(year,month)[1])
#        return datetime.date(year,month,day)


#class DataLoaderAnomaly():
#    def __init__(self, channels, startDate, endDate):
#        db = DBconnect(None)
#        self.db = db
#        self.channels = channels
#        self.startDate = startDate
#        self.endDate = endDate
#        self.currentDate = startDate
#        self.channelsSql = db.createChannelsSql(channels, None)
#        self.data = []
        #self.datePartFeatures = 2
#        self.datePartFeatures = 31


#    def getNextPeriod(self, days):
#        channels = self.channels
#        #need to get longest sequence
#        nextDate = self.add_day(self.currentDate, days)

#        hours = (nextDate - self.currentDate).days * 24
#        input = np.empty([hours,1,len(channels)+self.datePartFeatures])
#        target = np.empty([hours,1,len(channels)])
#        length = len(channels)
#        #Loop where we select all channels pr datetime
#        for x in range(hours +1):
#            logdateWhere = "logdate=dateadd(hh, " + str(x) + ",'" + str(self.currentDate) + "')"
#            df = self.db.getFrame(self.channelsSql + " and " + logdateWhere)
#            #dfTarget = self.db.getFrame("select datepart(hh, logdate) as hour, datepart(dw, logdate) as weekday, value, channel from hour where channel="+str(self.predictChan) + " and " + logdateWhere)
#            channels = self.channels.copy()
#            maxC = len(channels)
#            for i, row in df.iterrows():
#                chanOffset = maxC - len(channels)
#                for c in range(len(channels)):
#                    if row.channel == channels[c]:
#                        if x != 0:
#                            #Target is tomorrow data, and thus 1 x behind, and not filled at x = 0
#                            target[x-1,0,(c+ chanOffset)] = row.value
#                        if x < hours:
#                            if i == 0: 
#                                input[x,0,0] = row.weekday0
#                                input[x,0,1] = row.weekday1
#                                input[x,0,2] = row.weekday2
#                                input[x,0,3] = row.weekday3
#                                input[x,0,4] = row.weekday4
#                                input[x,0,5] = row.weekday5
#                                input[x,0,6] = row.weekday6
#                                input[x,0,7] = row.hh0
#                                input[x,0,8] = row.hh1
#                                input[x,0,9] = row.hh2
#                                input[x,0,10] = row.hh3
#                                input[x,0,11] = row.hh4
#                                input[x,0,12] = row.hh5
#                                input[x,0,13] = row.hh6
#                                input[x,0,14] = row.hh7
#                                input[x,0,15] = row.hh8
#                                input[x,0,16] = row.hh9
#                                input[x,0,17] = row.hh10
#                                input[x,0,18] = row.hh11
#                                input[x,0,19] = row.hh12
#                                input[x,0,20] = row.hh13
#                                input[x,0,21] = row.hh14
#                                input[x,0,22] = row.hh15
#                                input[x,0,23] = row.hh16
#                                input[x,0,24] = row.hh17
#                                input[x,0,25] = row.hh18
#                                input[x,0,26] = row.hh19
#                                input[x,0,27] = row.hh20
#                                input[x,0,28] = row.hh21
#                                input[x,0,29] = row.hh22
#                                input[x,0,30] = row.hh23
#                                input[x,0,(c+self.datePartFeatures + chanOffset)] = row.value
#                                del channels[c]
#                                break;
#                            else:
#                                input[x,0,(c+self.datePartFeatures + chanOffset)] = row.value
#                                del channels[c]
#                                break;
#                    #elif row.channel == self.predictChan:
#                    #    target[x,0,0] = row.value
#                    #    break;
        
#        #Get the last target
#        self.currentDate = nextDate
#        input = torch.from_numpy(input).float()
#        target = torch.from_numpy(target).float()
#        self.data.append([input,target])
#        return input, target

#    def getPeriodI(self, i):
#        return self.data[i]

#    def getRandomPeriod(self):
#        outI = np.random.choice(self.iArr, size=1, replace=False, p=None)
#        self.iArr.remove(outI)

#        return self.getPeriodI(outI[0])

#    def resetI(self, trainingSplit):
#        self.iArr = []
#        for i in range (trainingSplit):
#            self.iArr.append(i)

#    def add_day(self, sourcedate, days):
#        return sourcedate + datetime.timedelta(days)

#    def add_months(self, sourcedate,months):
#        month = sourcedate.month - 1 + months
#        year = sourcedate.year + month // 12
#        month = month % 12 + 1
#        day = min(sourcedate.day,calendar.monthrange(year,month)[1])
#        return datetime.date(year,month,day)


#class DataLoaderProduction():
#    def __init__(self, channels, startDate, endDate, config, saDefId, use_cuda):
#        db = DBconnect(config)
#        self.db = db
#        self.channels = channels
#        self.startDate = startDate
#        self.endDate = endDate
#        self.currentDate = startDate
#        self.channelsSql = db.createChannelsSql(channels, None)
#        self.data = []
#        self.datePartFeatures = 31
#        self.saDefId = saDefId
#        sql = "select * from SAExcludedDates where SmartAssistantDefId=" + str(saDefId)
#        df = db.getFrame(sql)
#        self.excludedDates = []
#        self.use_cuda = use_cuda
#        for i, row in df.iterrows():
#            currDate = row.StartDate
#            while currDate < row.EndDate:
#                self.excludedDates.append(currDate)
#                #currDate = self.add_day(currDate, 1)
#                currDate = currDate + datetime.timedelta(hours=1)

#        self.trainingSplit = (self.endDate -self.startDate).days*24

#    def getNextPeriod(self, days):
#        channels = self.channels
#        #need to get longest sequence
#        #nextDate = self.add_day(self.currentDate, days)
#        nextDate = self.currentDate  + datetime.timedelta(hours=1)

#        if nextDate in self.excludedDates:
#            #If date is excluded, then we move to next, and decrement trainingSplit. We could decremtn trainingSplit on excludedDates len
#            # but, it could contain future dates, so we only do it when a date is ignored.
#            self.currentDate = nextDate
#            self.trainingSplit = self.trainingSplit -1
#            return self.getNextPeriod(days)

#        hours = int((nextDate - self.currentDate).seconds/60/60)

#        input = np.empty([hours,1,len(channels)+self.datePartFeatures])
#        target = np.empty([hours,1,len(channels)])
#        channelOrder = []
#        length = len(channels)
#        #Loop where we select all channels pr datetime
#        for x in range(hours +1): #+1 to get the last target value - input[hours+1] will not get filled
#            logdateWhere = "logdate=dateadd(hh, " + str(x) + ",'" + str(self.currentDate) + "')"
#            df = self.db.getFrame(self.channelsSql + " and " + logdateWhere)
#            #dfTarget = self.db.getFrame("select datepart(hh, logdate) as hour, datepart(dw, logdate) as weekday, value, channel from hour where channel="+str(self.predictChan) + " and " + logdateWhere)
#            channels = self.channels.copy()
#            maxC = len(channels)
#            for i, row in df.iterrows():
#                chanOffset = maxC - len(channels)
#                for c in range(len(channels)):
#                    if row.channel == channels[c]:
#                        if x != 0:
#                            #Target is tomorrow data, and thus 1 x behind, and not filled at x = 0
#                            target[x-1,0,(c+ chanOffset)] = row.value
#                            if x == hours: #We are at the last target, only need to grap target here.
#                                del channels[c]
#                                break
#                        else:
#                            channelOrder.append(row.channel)

#                        if x < hours:
#                            if i == 0: 
#                                input[x,0,0] = row.weekday0
#                                input[x,0,1] = row.weekday1
#                                input[x,0,2] = row.weekday2
#                                input[x,0,3] = row.weekday3
#                                input[x,0,4] = row.weekday4
#                                input[x,0,5] = row.weekday5
#                                input[x,0,6] = row.weekday6
#                                input[x,0,7] = row.hh0
#                                input[x,0,8] = row.hh1
#                                input[x,0,9] = row.hh2
#                                input[x,0,10] = row.hh3
#                                input[x,0,11] = row.hh4
#                                input[x,0,12] = row.hh5
#                                input[x,0,13] = row.hh6
#                                input[x,0,14] = row.hh7
#                                input[x,0,15] = row.hh8
#                                input[x,0,16] = row.hh9
#                                input[x,0,17] = row.hh10
#                                input[x,0,18] = row.hh11
#                                input[x,0,19] = row.hh12
#                                input[x,0,20] = row.hh13
#                                input[x,0,21] = row.hh14
#                                input[x,0,22] = row.hh15
#                                input[x,0,23] = row.hh16
#                                input[x,0,24] = row.hh17
#                                input[x,0,25] = row.hh18
#                                input[x,0,26] = row.hh19
#                                input[x,0,27] = row.hh20
#                                input[x,0,28] = row.hh21
#                                input[x,0,29] = row.hh22
#                                input[x,0,30] = row.hh23
#                                input[x,0,(c+self.datePartFeatures + chanOffset)] = row.value
#                                del channels[c]
#                                break;
#                            else:
#                                input[x,0,(c+self.datePartFeatures + chanOffset)] = row.value
#                                del channels[c]
#                                break;
#                    #elif row.channel == self.predictChan:
#                    #    target[x,0,0] = row.value
#                    #    break;
        
#        self.lastDate = self.currentDate
#        self.currentDate = nextDate
#        input = torch.from_numpy(input).float()
#        target = torch.from_numpy(target).float()
#        if self.use_cuda:
#            input = input.cuda()
#            target = target.cuda()

#        self.data.append([input,target])
#        return input, target, channelOrder


#    def getPeriodI(self, i):
#        return self.data[i]

#    def getRandomPeriod(self):
#        outI = np.random.choice(self.iArr, size=1, replace=False, p=None)
#        self.iArr.remove(outI)

#        return self.getPeriodI(outI[0])

#    def resetI(self):
        
#        self.iArr = []
#        for i in range (self.trainingSplit):
#            self.iArr.append(i)

#    def add_day(self, sourcedate, days):
#        return sourcedate + datetime.timedelta(days)

#    def add_months(self, sourcedate,months):
#        month = sourcedate.month - 1 + months
#        year = sourcedate.year + month // 12
#        month = month % 12 + 1
#        day = min(sourcedate.day,calendar.monthrange(year,month)[1])
#        return datetime.date(year,month,day)


class DataLoaderWebservice():
    def __init__(self, startDate, endDate, serviceUrl, saDefId, use_cuda):
        requestUrl = serviceUrl + "/Services/DataService.ashx?type=anomaly&sadefId=" + str(saDefId) + "&startdate=" + str(startDate.strftime("%Y-%m-%dT%H:%M:%S")) + "&enddate=" + str(endDate.strftime("%Y-%m-%dT%H:%M:%S"))
        f = urlopen(requestUrl)
        dataLoader = json.loads(f.read())
        self.dataLoader = dataLoader
        self.data = []
        self.saDefId = saDefId
        self.use_cuda = use_cuda
        self.trainingSplit = len(dataLoader['inputs'])
        self.features_in = len(dataLoader['inputs'][0])
        self.features_out = len(dataLoader['targets'][0])
        self.currI = 0;

    def getNextPeriod(self):
        npInput = np.empty([1,1, self.features_in])
        npInput[0][0][:] =np.asarray(self.dataLoader['inputs'][self.currI])
        npTarget = np.empty([1,1, self.features_out])
        npTarget[0][0][:] = np.asarray(self.dataLoader['targets'][self.currI])

        input = torch.from_numpy(npInput).float()
        target = torch.from_numpy(npTarget).float()
        date = self.dataLoader['dates'][self.currI]
        self.currI = self.currI + 1
        if self.use_cuda:
            input = input.cuda()
            target = target.cuda()

        self.data.append([input,target])
        return input, target, date

    def getPeriodI(self, i):
        return self.data[i]

    def getRandomPeriod(self):
        outI = np.random.choice(self.iArr, size=1, replace=False, p=None)
        self.iArr.remove(outI)

        return self.getPeriodI(outI[0])

    def resetI(self):
        
        self.iArr = []
        for i in range (self.trainingSplit):
            self.iArr.append(i)

    def add_day(self, sourcedate, days):
        return sourcedate + datetime.timedelta(days)

    def add_months(self, sourcedate,months):
        month = sourcedate.month - 1 + months
        year = sourcedate.year + month // 12
        month = month % 12 + 1
        day = min(sourcedate.day,calendar.monthrange(year,month)[1])
        return datetime.date(year,month,day)