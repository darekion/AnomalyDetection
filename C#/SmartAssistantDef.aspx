<asp:ListView ID="SADefAnomalyGrid" runat="server" OnItemCanceling="SADefAnomalyGrid_ItemCanceling" OnItemEditing="SADefAnomalyGrid_ItemEditing"
OnItemDeleting="SADefAnomalyGrid_ItemDeleting" OnItemUpdating="SADefAnomalyGrid_ItemUpdating" OnItemDataBound="SADefAnomalyGridItemBound">
	<LayoutTemplate>
		<table class="listView bordered horizontalGrid">
			<tr>
				<td colspan="14"><asp:Label ID="AnomalySettingsLbl" runat="server" Text="SMART Assistant setup" CssClass="settingsHeader"></asp:Label></td>
			</tr>
			<tr>
				<td></td>
				<td></td>
				<td></td>
				<td class="width300"><asp:Label ID="ChannelHeaderLbl" runat="server" Text="Channel"></asp:Label></td>
				<td class="width100"><asp:Label ID="AnomalyStartDateHeaderLbl" runat="server" Text="Start date for training"></asp:Label></td>
				<td class="width100"><asp:Label ID="AnomalyThresholdHeaderLbl" runat="server" Text="Accuracy threshold (% deviation from real value)"></asp:Label></td>
				<td class="width175"><asp:Label ID="AnomalyExcludedDatesHeaderLbl" runat="server" Text="Excluded date intervals"></asp:Label></td>
				<td class="width100"><asp:Label runat="server" ID="AnomalyNewExcludedDatesHeaderLbl" Text="New excluded date interval"></asp:Label></td>
				<td class="width175"><asp:Label runat="server" ID="AnomalyOutputChannelsHeaderLbl" Text=""></asp:Label></td>
				<td class="width175"><asp:Label ID="SAGroupHeaderLbl" runat="server" Text="Group"></asp:Label></td>
				<td><asp:Label runat="server" ID="ChannelCountHeaderLbl" Text="Monitored channels"></asp:Label></td>
				<td><asp:Label ID="OwnerHeaderLbl" runat="server" Text="Owner"></asp:Label></td>
				<td><asp:Label ID="DailyMailHeaderLbl" runat="server" Text="Mail method"></asp:Label></td>
				<td><asp:Label ID="CommentHeaderLbl" runat="server" Text="Comment"></asp:Label></td>
			</tr>
			<tr runat="server" id="itemPlaceHolder">
			</tr>
			<tr>
			
			</tr>
		</table>
	</LayoutTemplate>
	<ItemTemplate>
			<tr>
				<td><asp:Button runat="server" ID="EditChannelBtn" Text="Edit" CommandName="Edit" CssClass="editImg btnImg"/></td>
				<td><asp:Button runat="server" ID="DeleteChannelBtn" Text="Delete" CommandName="Delete" CssClass="deleteImg btnImg" OnClientClick="return confirm('Are you sure you want to delete this eventdef?')"/></td>
				<td></td>
				<td>
					<asp:Label runat="server" ID="SmartAssistantDefIdLbl" Visible = "false" Text='<%# Eval("SmartAssistantDefId") %>'></asp:Label>
					<asp:Label runat="server" ID="ChannelAnomalyLbl1" Text="Data basis"></asp:Label>
					<asp:Label runat="server" ID="ChannelLabel" Text='<%# Eval("Calculation") %>'></asp:Label>
					<br />
					<asp:Label runat="server" ID="ChannelAnomalyLbl2" Text="Monitored"></asp:Label>
					<asp:Label runat="server" ID="ChannelLabel2"></asp:Label>
				</td>
				<td>
					<asp:Label runat="server" ID="AnomalyStartDateLbl" Text='<%# Eval("ValueType") %>'></asp:Label>
				</td>
				<td>
					<asp:Label runat="server" ID="AnomalyThresholdLbl" Text='<%# Eval("ValueLimit") %>'></asp:Label>
				</td>
				<td><asp:Label runat="server" ID="AnomalyExcludedDatesLbl"></asp:Label></td>
				<td><asp:Label runat="server" ID="AnomalyNewExcludedDatesLbl"></asp:Label></td>
				<td><asp:Label runat="server" ID="AnomalyOutputChannelsLbl"></asp:Label></td>
				<td><asp:Label runat="server" ID="SAGroupLbl" Text='<%# Eval("SAGroup") %>'></asp:Label></td>
				<td>
					<asp:Label runat="server" ID="ChannelCountLbl"></asp:Label>
					<br />
					<asp:Label runat="server" ID="ChannelCountLbl2"></asp:Label>
				</td>
				<td>
					<asp:Label runat="server" ID="OwnerIdLbl" Text='<%# Eval("OwnerId") %>' Visible="false"></asp:Label>
					<asp:Label runat="server" ID="OwnerLbl" Text='<%# Eval("GroupName") %>'></asp:Label>
				</td>
				<td>
					<asp:Label runat="server" ID="DailyMailDBTxtLbl" Visible="false" Text='<%# Eval("DailyMail") %>'></asp:Label>
					<asp:Label runat="server" ID="DailyMailLbl" ></asp:Label>
			
				</td>
				<td><asp:Label runat="server" ID="CommentLbl" Text='<%# Eval("Comment") %>'></asp:Label></td>
	
			</tr>
	</ItemTemplate>
	<EditItemTemplate>
		<tr>
			<td><asp:Button runat="server" ID="CancelEditBtn" Text="Cancel" CommandName="Cancel" CssClass="btnImg closeImg"/></td>
			<td><asp:Button runat="server" ID="UpdateChannelBtn" Text="Update" CommandName="Update" CssClass="btnImg saveImg"/></td>
			<td><asp:Button runat="server" ID="CalculationShowBtn" Text="Vis kanaler" OnClick="CalculationShowMultiEditBtn_click" CssClass="btnImg"/></td>
			<td>
				<asp:Label runat="server" ID="SmartAssistantDefIdLbl" Visible="false" Text='<%# Eval("SmartAssistantDefId") %>'></asp:Label>
				<asp:Label runat="server" ID="ChannelAnomalyLbl1" Text="Data basis"></asp:Label>
				<asp:TextBox runat="server" ID="ChannelTextbox" CssClass="channel" Text='<%# Eval("Calculation") %>'></asp:TextBox>
				<asp:Label runat="server" ID="ChannelLabel" Visible="false" Text='<%# Eval("Calculation") %>'></asp:Label>
				<asp:Label runat="server" ID="ChannelAnomalyLbl2" Text="Monitored"></asp:Label>
				<asp:TextBox runat="server" ID="ChannelTextbox2" CssClass="channel"></asp:TextBox>
				<asp:Label runat="server" ID="ChannelLabel2" Visible="false"></asp:Label>
			</td>
			<td>
				<asp:TextBox runat="server" ID="AnomalyStartDateTextbox" CssClass="date startDate" Text='<%# Eval("ValueType") %>'></asp:TextBox>
			</td>
			<td><asp:TextBox runat="server" ID="ValueLimitTextbox" Text='<%# Eval("ValueLimit") %>'></asp:TextBox></td>
			<td>
				<asp:DropDownList runat="server" ID="ExcludedDatesDDL" CssClass="excludedDatesDDL"></asp:DropDownList>
				<asp:Label runat="server" ID="ExcludedDatesSelectedLbl" CssClass="hidden excludedDateLbl"></asp:Label>
				<asp:Button runat="server" ID="RemoveExcludedDateBtn" Text="Remove Date" OnClick="RemoveExcludedDateBtn_click" />
			</td>
			<td>
				<asp:TextBox runat="server" ID="NewExcludedStartDateTextbox" CssClass="date startDate"></asp:TextBox>
				<asp:TextBox runat="server" ID="NewExcludedEndDateTextbox" CssClass="date endDate"></asp:TextBox>
				<asp:Button runat="server" ID="NewExcludedDateBtn" Text="Add Date" OnClick="NewExcludedDateBtn_click"/>
			</td>
			<td>
				<asp:TextBox runat="server" ID="AnomalyOutputChannelsTextbox"></asp:TextBox>
				<asp:Button runat="server" ID="GenerateOutputChannelsBtn" Text="Generate output channels" OnClick="GenerateOutputChannelsBtn_click" />
			</td>
			<td><asp:TextBox runat="server" ID="SAGroupTextbox" Text='<%# Eval("SAGroup") %>'></asp:TextBox></td>
			<td>
				<asp:Label runat="server" ID="ChannelCountLbl"></asp:Label>
				<br />
				<asp:Label runat="server" ID="ChannelCountLbl2"></asp:Label>
			</td>
			<td>
				<asp:Label runat="server" ID="OwnerIdLbl" Text='<%# Eval("OwnerId") %>' Visible="false"></asp:Label>
				<asp:Label runat="server" ID="OwnerLabel" Text='<%# Eval("GroupName") %>'></asp:Label>
				<asp:DropDownList runat="server" ID="OwnerDDL" Visible="false"></asp:DropDownList>
			</td>
			<td>
				<asp:Label runat="server" ID="DailyMailDBTxtLbl" Text='<%# Eval("DailyMail") %>' Visible="false"></asp:Label>
				<asp:DropDownList runat="server" ID="DailyMailDDL"></asp:DropDownList>
			</td>
			<td><asp:TextBox runat="server" ID="CommentTextbox" Text='<%# Eval("Comment") %>'></asp:TextBox></td>
		</tr>
	</EditItemTemplate>
	<InsertItemTemplate>
			<tr>
			</tr>   
	</InsertItemTemplate>
</asp:ListView>
<br id="AnomalyBr1" runat="server"/>
<br id="AnomalyBr2" runat="server"/>
