        #region Anomaly listview events
        protected void fillSADefAnomalyGrid()
        {
            string sql = "select SmartAssistantDef.smartassistantdefid, valuelimit, DataResolution, ValueType, SmartAssistantDef.Calculation, " +
                        "OwnerId, SAGroup, DailyMail, SmartAssistantDef.comment, GroupName, ParamValue as Calculation2 " +
                        "from SmartAssistantDef inner join UserGroups on ownerid = GroupId " +
                        "inner join SmartAssistantDefExtra on SmartAssistantDef.smartassistantdefid=SmartAssistantDefExtra.smartassistantdefid " +
                      //  "inner join SAExcludedDates on SmartAssistantDef.smartassistantdefid = SAExcludedDates.smartassistantdefid " +
                        "where ruletype='anomaly' " + whereMultiSql + " and ParamName='Calculation2' order by SAGroup";
           
            //EventGrid.InsertItemPosition = InsertItemPosition.LastItem;
            SqlDataSource datasource = db.getSqlDataSource(sql);
            datasource.DataSourceMode = SqlDataSourceMode.DataReader;
            SADefAnomalyGrid.InsertItemPosition = InsertItemPosition.FirstItem;
            SADefAnomalyGrid.DataSource = datasource;
            SADefAnomalyGrid.DataBind();
            if (SADefAnomalyGrid.Items.Count > 0)
            {
                setSADefAnomalyGridsText();
                SADefAnomalyGrid.Visible = true;
                warningBr1.Visible = true;
                warningBr2.Visible = true;
            }
            else
            {
                SADefAnomalyGrid.Visible = false;
                warningBr1.Visible = false;
                warningBr2.Visible = false;
            }
        }

        //event for when items are bound to the EventDefGrid, so the dropdownlists can be filled
        protected void SADefAnomalyGridItemBound(object sender, ListViewItemEventArgs e)
        {

            List<ListItem> DailyMailDDL = Session["DailyMail"] as List<ListItem>;
            //string[] missArr = (e.Item.FindControl("CalculationLabel") as Label).Text.Split(' ');
            string smartAssistantDefId = (e.Item.FindControl("SmartAssistantDefIdLbl") as Label).Text;

            string channelText = (e.Item.FindControl("ChannelLabel") as Label).Text;
            channelText = channelText.Trim();
            Label channelCountLbl = e.Item.FindControl("ChannelCountLbl") as Label;
            channelCountLbl.Text = countChannelsForRule(channelText);

            string channelText2 = "", outputChannels = "";
            db.newCommand("select paramname, paramvalue from SmartAssistantDefExtra where SmartAssistantDefId=@SmartAssistantDefId");
            db.addParameter("@SmartAssistantDefId", smartAssistantDefId);
            db.executeSelect();
            List<object> extraParams = db.readAll();
            if (extraParams != null)
            {
                for(int i = 0; i < extraParams.Count; i++) {
                    List<object> param = extraParams[i] as List<object>;
                    switch(param[0].ToString().ToLower())
                    {
                        case "calculation2":
                            channelText2 = param[1].ToString().Trim();
                            Label channelCountLbl2 = e.Item.FindControl("ChannelCountLbl2") as Label;
                            channelCountLbl2.Text = countChannelsForRule(channelText2);
                            break;
                        case "outputchannels":
                            outputChannels = param[1].ToString();
                            break;
                    }
                     
                }
            }

            setLabelText((e.Item.FindControl("ChannelAnomalyLbl1") as Label), "ChannelAnomalyLbl1");
            setLabelText((e.Item.FindControl("ChannelAnomalyLbl2") as Label), "ChannelAnomalyLbl2");

            if (SADefAnomalyGrid.EditIndex == e.Item.DataItemIndex)
            {
                //Fill dropdownlists for the editting item.
                fillDropDownList(e.Item, "DailyMail", Session["DailyMail"] as List<ListItem>);

                (e.Item.FindControl("CancelEditBtn") as Button).Text = languageDictionary["CancelText"];
                (e.Item.FindControl("UpdateChannelBtn") as Button).Text = languageDictionary["UpdateText"];
                (e.Item.FindControl("DailyMailDDL") as DropDownList).SelectedIndex = Convert.ToInt32(Convert.ToBoolean((e.Item.FindControl("DailyMailDBTxtLbl") as Label).Text));
                (e.Item.FindControl("ChannelTextbox2") as TextBox).Text = channelText2;
                (e.Item.FindControl("AnomalyOutputChannelsTextbox") as TextBox).Text = outputChannels;
                //(e.Item.FindControl("ChannelTextbox") as TextBox).Text = channelText;
                setButtonText((e.Item.FindControl("RemoveExcludedDateBtn") as Button), "RemoveExcludedDateBtn");
                setButtonText((e.Item.FindControl("NewExcludedDateBtn") as Button), "NewExcludedDateBtn");

                DropDownList excludedDatesDDL = (e.Item.FindControl("ExcludedDatesDDL") as DropDownList);
                db.newCommand("select SAExcludedDatesId, startdate, enddate from SAExcludedDates where smartAssistantDefId=@smartAssistantDefId");
                db.addParameter("@smartAssistantDefId", smartAssistantDefId);
                db.executeSelect();
                List<object> excludedDates = db.readAll();
                if (excludedDates != null)
                {
                    for (int i = 0; i < excludedDates.Count; i++)
                    {
                        List<object> dateRow = excludedDates[i] as List<object>;
                        //skal måske genoverveje dato formattet ?
                        excludedDatesDDL.Items.Add(new ListItem(Convert.ToDateTime(dateRow[1]).ToString("yyyy-MM-dd") + " - " + Convert.ToDateTime(dateRow[2]).ToString("yyyy-MM-dd"), dateRow[0].ToString())); 
                    }
                } 

                //setup admin part
                fillDropDownList(e.Item, "Owner", Session["Groups"] as List<ListItem>); //DDL must be filled for the update function, its hidden for non admins
                if (isAdmin)
                {
                    (e.Item.FindControl("OwnerLabel") as Label).Visible = false;
                    (e.Item.FindControl("OwnerDDL") as DropDownList).Visible = true;
                }
            }
            else
            {
                
                if (isAdmin || userGroupIds.Contains(Convert.ToInt32((e.Item.FindControl("OwnerIdLbl") as Label).Text)))
                {
                    (e.Item.FindControl("EditChannelBtn") as Button).Text = languageDictionary["EditText"];
                    (e.Item.FindControl("DeleteChannelBtn") as Button).Text = languageDictionary["DeleteText"];
                    (e.Item.FindControl("DeleteChannelBtn") as Button).OnClientClick = "return confirm('" + languageDictionary["ConfirmDeleteSmartAssistantDef"] + " ?')";
                }
                else
                {
                    (e.Item.FindControl("EditChannelBtn") as Button).Visible = false;
                    (e.Item.FindControl("DeleteChannelBtn") as Button).Visible = false;
                }

                (e.Item.FindControl("ChannelLabel2") as Label).Text = channelText2;
                (e.Item.FindControl("AnomalyOutputChannelsLbl") as Label).Text = outputChannels;

                db.newCommand("select count(*) from SAExcludedDates where smartAssistantDefId=@smartAssistantDefId");
                db.addParameter("@smartAssistantDefId", smartAssistantDefId);
                object temp = db.executeScalar();
                if (temp != null && temp != DBNull.Value)
                {
                    (e.Item.FindControl("AnomalyExcludedDatesLbl") as Label).Text = temp.ToString();
                }

                //in database its a bit (false/true), so we must convert to boolean first, before converting to int.
                (e.Item.FindControl("DailyMailLbl") as Label).Text = DailyMailDDL[Convert.ToInt32(Convert.ToBoolean((e.Item.FindControl("DailyMailDBTxtLbl") as Label).Text))].Value;
            }
        }

        protected void SADefAnomalyGrid_ItemCanceling(object sender, ListViewCancelEventArgs e)
        {
            SADefAnomalyGrid.EditIndex = -1;
        }

        protected void SADefAnomalyGrid_ItemEditing(object sender, ListViewEditEventArgs e)
        {
            SADefAnomalyGrid.EditIndex = e.NewEditIndex;
        }

        protected void SADefAnomalyGrid_ItemDeleting(object sender, ListViewDeleteEventArgs e)
        {
            SADefAnomalyGrid.EditIndex = -1;
            deleteSA((SADefAnomalyGrid.Items[e.ItemIndex].FindControl("SmartAssistantDefIdLbl") as Label));
        }

        protected void SADefAnomalyGrid_ItemUpdating(object sender, ListViewUpdateEventArgs e)
        {
            ListViewDataItem saDefRow = SADefAnomalyGrid.Items[e.ItemIndex];
            updateSADef(saDefRow);
            SADefAnomalyGrid.EditIndex = -1;
            
        }

        private void updateSADef(Control saDefRow)
        {
            SmartAssistantObject saObj = new SmartAssistantObject();
            //saObj.Channel = Convert.ToInt32(getSelectedChannelFromChannelTextbox(saDefRow.FindControl("ChannelTextbox") as TextBox));
            saObj.Group = (saDefRow.FindControl("SAGroupTextbox") as TextBox).Text;
            saObj.Comment = (saDefRow.FindControl("CommentTextbox") as TextBox).Text;
            saObj.DailyMail = Convert.ToBoolean((saDefRow.FindControl("DailyMailDDL") as DropDownList).SelectedIndex);
            saObj.SADefId = Convert.ToInt32((saDefRow.FindControl("SmartAssistantDefIdLbl") as Label).Text);
            if (isAdmin)
            {
                saObj.OwnerId = Convert.ToInt32((saDefRow.FindControl("OwnerDDL") as DropDownList).SelectedValue);
            }
            else
            {
                saObj.OwnerId = Convert.ToInt32((saDefRow.FindControl("OwnerIdLbl") as Label).Text);
            }
            saObj.DataResolution = 0;
            saObj.ValueLimit = Convert.ToInt32((saDefRow.FindControl("ValueLimitTextbox") as TextBox).Text);
            saObj.ValueType = (saDefRow.FindControl("AnomalyStartDateTextbox") as TextBox).Text;
            saObj.Calculation = (saDefRow.FindControl("ChannelTextbox") as TextBox).Text;
            saObj.ExtraParamDictionary.Add("Calculation2", (saDefRow.FindControl("ChannelTextbox2") as TextBox).Text);
            saObj.ExtraParamDictionary.Add("OutputChannels", (saDefRow.FindControl("AnomalyOutputChannelsTextbox") as TextBox).Text);
            updateOrInsertSADef(saObj, true);
        }


        private void setSADefAnomalyGridsText()
        {
            try
            {
                //SADefAnomalyGrid
                setLabelText((SADefAnomalyGrid.FindControl("AnomalySettingsLbl") as Label), "AnomalySettingsText");
                setLabelText((SADefAnomalyGrid.FindControl("ChannelHeaderLbl") as Label), "ChannelText");
                setLabelText((SADefAnomalyGrid.FindControl("AnomalyStartDateHeaderLbl") as Label), "AnomalyStartDateHeaderLbl");
                setLabelText((SADefAnomalyGrid.FindControl("AnomalyThresholdHeaderLbl") as Label), "AnomalyThresholdHeaderLbl");
                setLabelText((SADefAnomalyGrid.FindControl("AnomalyExcludedDatesHeaderLbl") as Label), "AnomalyExcludedDatesHeaderLbl");
                setLabelText((SADefAnomalyGrid.FindControl("AnomalyNewExcludedDatesHeaderLbl") as Label), "AnomalyNewExcludedDatesHeaderLbl");
                
                setLabelText((SADefAnomalyGrid.FindControl("OwnerHeaderLbl") as Label), "OwnerText");
                setLabelText((SADefAnomalyGrid.FindControl("SAGroupHeaderLbl") as Label), "SAGroupText");
                setLabelText((SADefAnomalyGrid.FindControl("DailyMailHeaderLbl") as Label), "MailMethodText");
                setLabelText((SADefAnomalyGrid.FindControl("CommentHeaderLbl") as Label), "CommentText");
                setLabelText((SADefAnomalyGrid.FindControl("ChannelCountHeaderLbl") as Label), "ChannelCountHeaderLbl");
                setLabelText((SADefAnomalyGrid.FindControl("AnomalyOutputChannelsHeaderLbl") as Label), "AnomalyOutputChannelsHeaderLbl");
                
            }
            catch (Exception ex)
            {
                //handle missing value
                ClsLogFile.logEvent("Misc.log", "Error during setSADefAnomalyGridsText in SmartAssitantDef.aspx, Ex msg: " + ex.Message + " Stack trace: " + ex.StackTrace);
            }
        }

        protected void NewExcludedDateBtn_click(object sender, EventArgs e)
        {
            try
            {
                Control parent = (sender as Control).Parent;
                TextBox startDateTextbox = parent.FindControl("NewExcludedStartDateTextbox") as TextBox;
                TextBox endDateTextbox = parent.FindControl("NewExcludedEndDateTextbox") as TextBox;
                Label smartAssistantDefIdLbl = parent.FindControl("SmartAssistantDefIdLbl") as Label;
                db.newCommand("insert into SAExcludedDates (SmartAssistantDefId, StartDate, EndDate) values (@SmartAssistantDefId, @StartDate, @EndDate)");
                db.addParameter("@SmartAssistantDefId", smartAssistantDefIdLbl.Text);
                db.addParameter("@StartDate", startDateTextbox.Text);
                db.addParameter("@EndDate", endDateTextbox.Text);
                db.executeNonQuery();
            }
            catch (Exception ex)
            {
                ClsLogFile.logEvent(logFilename, "Got exception in NewExcludedDateBtn_click() ex msg: " + ex.Message + " stacktrace: " + ex.StackTrace);
            }
        }

        protected void RemoveExcludedDateBtn_click(object sender, EventArgs e)
        {
            Control parent = (sender as Control).Parent;
            DropDownList ExcludedDatesDDL = parent.FindControl("ExcludedDatesDDL") as DropDownList;
            db.newCommand("delete from SAExcludedDates where SAExcludedDatesId=@SAExcludedDatesId");
            db.addParameter("@SAExcludedDatesId", ExcludedDatesDDL.SelectedValue);
            db.executeNonQuery();
        }

        protected void GenerateOutputChannelsBtn_click(object sender, EventArgs e)
        {
            try
            {
                Control parent = (sender as Control).Parent;
                TextBox outputChannelsTextbox = parent.FindControl("AnomalyOutputChannelsTextbox") as TextBox;
                
                string sql;
                if (string.IsNullOrEmpty(outputChannelsTextbox.Text))
                {
                    TextBox targetChannels = parent.FindControl("ChannelTextbox2") as TextBox;
                    string calc = targetChannels.Text.Trim().ToLower();
                    if (calc.Contains("description") || calc.Contains("calculation") || calc.Contains("io_tagname") || calc.Contains("io_tagmode") ||
                    calc.Contains("channeltype") || calc.Contains("comment") || calc.Contains("unit"))
                    {
                        sql = "select * from histdef where (" + targetChannels.Text.Replace('*', '%') + ") and enabledFlag = 1";
                    }
                    else
                    {
                        sql = "select * from histdef where description like '" + targetChannels.Text.Replace('*', '%') + "' and enabledFlag = 1";
                    }

                    List<HistDefObject> histdefs = db.readHistDef(sql);
                   // ClsLogFile.logEvent(logFilename, "Got histdef length: " + histdefs);
                    sql = "select top 1 channel from histdef order by channel desc";
                    db.newCommand(sql);
                    int maxChannel = Convert.ToInt32(db.executeScalar());
                    List<int> outputChannels = new List<int>();
                    foreach (HistDefObject hist in histdefs)
                    {
                        maxChannel++;
                        sql = "insert into histdef (Channel, Description, Unit, Minimum, Maximum, Calculation, IO_TagName, IO_TagMode, " +
                            " AmountPrHour, CalcDevoidData, CollectorID, LatestOkValueHour, ChannelType, ConcentrateDataHole, EnabledFlag, Comment) values " +
                            "(@Channel, @Description, @Unit, @Minimum, @Maximum, @Calculation, @IO_TagName, @IO_TagMode, " +
                            " @AmountPrHour, @CalcDevoidData, @CollectorID, @LatestOkValueHour, @ChannelType, @ConcentrateDataHole, @EnabledFlag, @Comment)";


                        db.newCommand(sql);
                        db.addParameter("@Channel", maxChannel);
                        db.addParameter("@Description", hist.Description + " SARuleId " + (parent.FindControl("SmartAssistantDefIdLbl") as Label).Text); //we only allow the shown characters in the description
                        db.addParameter("@Unit", hist.Unit);
                        db.addParameter("@Minimum", hist.Minimum);
                        db.addParameter("@Maximum", hist.Maximum);
                        db.addParameter("@Calculation", "SmartAssistantStoredData " + hist.Channel);
                        db.addParameter("@IO_TagName", "");
                        db.addParameter("@IO_TagMode", "");
                        db.addParameter("@AmountPrHour", hist.AmountPrHour);
                        db.addParameter("@CalcDevoidData", hist.CalcDevoidData);
                        db.addParameter("@CollectorID", "");
                        db.addParameter("@ChannelType", hist.ChannelType);
                        db.addParameter("@Comment", ""); //we only allow the shown characters in the comment
                        db.addParameter("@ConcentrateDataHole", hist.ConcentrateDataHole);
                        db.addParameter("@EnabledFlag", hist.EnabledFlag);
                        db.addParameter("@LatestOkValueHour", hist.LatestOkValueHour);
                        db.executeNonQuery();
                        outputChannels.Add(maxChannel);
                        
                    }
                    string result = "";
                    foreach (int channel in outputChannels)
                    {
                        result += channel + "; ";
                    }
                    outputChannelsTextbox.Text = result.Remove(result.Length - 1);
                    updateSADef(parent);
                }
                else
                {
                    //don't want to do this by mistake, so only do it if output channels are empty
                }
            }
            catch (Exception ex)
            {
                ClsLogFile.logEvent(logFilename, "Got exception in GenerateOutputChannelsBtn_click() with ex msg: " + ex.Message + " stack trace: " + ex.StackTrace);
            }
        }
        #endregion

        private void updateOrInsertSADef(SmartAssistantObject saObj, bool isUpdate)
        {
            try
            {
                string sql;
                if (isUpdate)
                {
                    sql = "update smartassistantdef set ValueLimit=@ValueLimit, DataResolution=@DataResolution, " +
                        "ValueType=@ValueType, SAGroup=@SAGroup, DailyMail=@DailyMail, Comment=@Comment, OwnerId=@OwnerId, Calculation=@Calculation " +
                        "where SmartAssistantDefId=@SmartAssistantDefId";
                }
                else
                {
                    sql = "insert into smartassistantdef (ValueLimit, DataResolution, ValueType, SAGroup, OwnerId, DailyMail, Comment, " + 
                        "Calculation, RuleType) values (@ValueLimit, @DataResolution, @ValueType, @SAGroup, @OwnerId, @DailyMail, " + 
                        "@Comment, @Calculation, @RuleType)";
                }

                db.newCommand(sql);
                db.addParameter("@ValueLimit", saObj.ValueLimit);
                db.addParameter("@ValueType", saObj.ValueType);
                db.addParameter("@DataResolution", saObj.DataResolution);
                db.addParameter("@SAGroup", saObj.Group);
                db.addParameter("@DailyMail", saObj.DailyMail);
                db.addParameter("@Comment", saObj.Comment);
                db.addParameter("@OwnerId", saObj.OwnerId);
                db.addParameter("@Calculation", saObj.Calculation);
                int id;
                if (isUpdate)
                {
                    db.addParameter("@SmartAssistantDefId", saObj.SADefId);
                    db.executeNonQuery();
                    id = saObj.SADefId;
                }
                else
                {
                    db.addParameter("@RuleType", saObj.RuleType);
                    id = db.executeNonQueryAndReturnId();
                }
                
                
                
                if (saObj.ExtraParamDictionary.Count > 0)
                {
                    
                    foreach (string key in saObj.ExtraParamDictionary.Keys.ToArray())
                    {
                        bool checkExists = isUpdate;
                        bool exists = false;
                        if(checkExists) 
                        {
                            db.newCommand("select count(*) from smartassistantdefExtra where smartassistantdefId=@smartassistantdefid and paramname=@paramname");
                            db.addParameter("@smartassistantdefid", id);
                            db.addParameter("@paramname", key);
                            object temp = db.executeScalar();
                            if (Convert.ToInt32(temp) > 0)
                            {
                                exists = true;
                            }
                        }
                        if (exists)
                        {
                            sql = "update smartassistantdefExtra set paramvalue=@paramvalue where smartassistantdefId=@smartassistantdefid and paramname=@paramname";
                        }
                        else
                        {
                            sql = "insert into smartassistantdefExtra (smartassistantdefId, paramname, paramvalue) values (@smartassistantdefid, @paramname, @paramvalue)";
                        }
                        db.newCommand(sql);
                        db.addParameter("@smartassistantdefid", id);
                        db.addParameter("@paramname", key);
                        db.addParameter("@paramvalue", saObj.ExtraParamDictionary[key]);
                        db.executeNonQuery();
                    }
                    db.newCommand(sql);
                }
            }
            catch (Exception ex)
            {
                ClsLogFile.logEvent(logFilename, "Got exception in updateOrInsertSADef(), with ex msg: " + ex.Message + " and stacktrace: " + ex.StackTrace); 
            }
        }