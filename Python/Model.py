import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
import numpy as np

class lstmNet(nn.Module):
    def __init__(self,features_in, features_out, num_layers, learning_rate, dropout, weight_decay, hidden_size):
        super(lstmNet, self).__init__()
        self.hidden_size = hidden_size
        self.lstm = nn.LSTM(features_in,hidden_size =self.hidden_size, num_layers=num_layers, dropout=dropout)
        self.features_in = features_in
        self.num_layers = num_layers
        self.criterion = nn.MSELoss()
        self.linear = nn.Linear(self.hidden_size, features_out)
        self.layerNorm = nn.LayerNorm(features_in)
        self.optimizer = torch.optim.SGD(self.parameters(),learning_rate,weight_decay=weight_decay)
        
    def forward(self,x):
        xNormed = self.layerNorm(x)
        output, self.hidden = self.lstm(xNormed)
        output = self.linear(output)
        return output 

    def initHidden(self, use_cuda):
        if use_cuda:
            self.hidden = (torch.zeros(self.num_layers,1, self.hidden_size, dtype=torch.float).cuda(),torch.zeros(self.num_layers,1, self.hidden_size, dtype=torch.float).cuda())
        else:
            self.hidden = (torch.zeros(self.num_layers,1, self.hidden_size, dtype=torch.float),torch.zeros(self.num_layers,1, self.hidden_size, dtype=torch.float))

    def save(self, path):
        torch.save({
            'model_state_dict': self.state_dict(),
            'optimizer_state_dict': self.optimizer.state_dict(),
            'model_hidden': self.hidden
            }, path)

    def load(self, path, use_cuda=False):
        if use_cuda:
            checkpoint = torch.load(path)
        else:
            checkpoint = torch.load(path,map_location='cpu')

        self.load_state_dict(checkpoint['model_state_dict'])
        self.optimizer.load_state_dict(checkpoint['optimizer_state_dict'])
        self.hidden = checkpoint['model_hidden']

#class lrModel(torch.nn.Module): 
  
#    def __init__(self, features, learning_rate, weight_decay): 
#        super(lrModel, self).__init__() 
#        self.linear = torch.nn.Linear(features, 1)  # One in and one out 
#        self.criterion = torch.nn.MSELoss() 
#        #self.criterion = nn.L1Loss()
#        self.optimizer = torch.optim.SGD(self.parameters(),learning_rate,weight_decay=weight_decay) 
#        #self.optimizer = torch.optim.Adam(self.parameters(),learning_rate)

#    def forward(self, x): 
#        y_pred = self.linear(x) 
#        return y_pred 