(function($)
{
	$.widget("custom.confirmdialog", $.ui.dialog,
	{
		_mainTimeoutID: undefined,
		_checkTimeoutID: undefined,
		options:
		{
			dialogType: ConfirmDialogTypes.ConfirmEmail,
			position: { my: "center", at: "center", of: ".account-form.register" },
			autoOpen: false,
			modal: true,
			resizable: false,
			title: "Notification message",
			model: null,
			buttons:
			[
				{
					id: "ui-ok-button",
					text: "OK",
					icon: "ui-icon-check",
					click: function(event)
					{
						$(".ui-confirmdialog").trigger("dialog-ok", event);
					}
				},
				{
					id: "ui-cancel-button",
					text: "Cancel",
					icon: "ui-icon-closethick",
					click: function(event)
					{
						$(".ui-confirmdialog").trigger("dialog-cancel", event);
					}
				},
				{
					id: "ui-send-button",
					text: "Generate new code",
					icon: "ui-icon-play",
					click: function(event)
					{
						$(".ui-confirmdialog").trigger("dialog-new-code", event);
					}
				},
			]
		},
		_create: function()
		{
			this._addClass("ui-confirmdialog");
			this._addClass("ui-confirm-dialog-default-content");
			this._addClass("ui-confirm-dialog-error-content");
			$.extend(this.options.classes,
			{
				"ui-confirmdialog": "ui-corner-all",
				"ui-dialog-titlebar": "ui-confirmdialog-titlebar ui-corner-all",
				"ui-dialog-titlebar-close": "ui-confirmdialog-titlebar-close",
				"ui-dialog-content": "ui-confirm-dialog-content",
				"ui-dialog-buttonpane": "ui-confirmdialog-buttonpane",
				"ui-dialog-buttonset": "ui-confirmdialog-buttonset",
				"ui-dialog-buttons": "ui-confirmdialog-buttons"
			});
			const that = this;
			this._on(this.element,
			{
				"dialog-ok": function(event)
				{
					if(that.options.dialogType == ConfirmDialogTypes.ConfirmPhoneNumber
						&& that.options.model.PhoneConfirmationState == ConfirmationStates.WaitForConfirmation
						&& $("#confirm-code-input").text() === that.options.model.PhoneVerificationCode)
					{
						that._clearMainTimer();
						that.options.model.PhoneConfirmationState = ConfirmationStates.Complete;
						window.location.href = `${window.location.origin}/UserAccount/Login?${modelToQueryString(that.options.model)}`;
						that.close(event);
						return false;
					}
					//that._setErrorContentVisibility();
				},
				"dialog-cancel": function(event)
				{
					that._clearMainTimer();
					that.close(event);
				},
				"dialog-new-code": function(event)
				{
					const action = that.options.dialogType == ConfirmDialogTypes.ConfirmPhoneNumber ? "SendNewVerificationCode" : "SendConfirmationLinkMail";
					that._postConfirmResult(action);
					that._restartMainTimer();
				}
			});
			this._super();
		},
		_setOptions: function(options)
		{
			this._setButtonsAndTimer(options);
			this._super();
		},
		open: function()
		{
			this._setButtonsAndTimer(this.options);
			$(".ui-confirmdialog-titlebar-close").css("display", "none");
			this._super();
		},
		_postConfirmResult: function(action)
		{
			const urlAction = this.window.location.origin + "/UserAccount/" + action;
			const jsonData = JSON.stringify(this.options.model);
			$.ajax({
				method: "POST",
				url: urlAction,
				data: jsonData,
				dataType: "json",
				contentType: "application/json; charset=utf-8"
			});
		},
		_setButtonsAndTimer: function(options)
		{
			const that = this;
			const buttonOK = $("#ui-ok-button");
			const buttonCancel = $("#ui-cancel-button");
			const buttonSend = $("#ui-send-button");
			var setTimeout = false;
			try
			{
				const textNode = $(buttonSend).contents()?.filter(function() { return this.nodeType === 3; })?.[0];
				if(options.dialogType === ConfirmDialogTypes.ConfirmPhoneNumber)
				{
					setTimeout = options.model.PhoneConfirmationState == ConfirmationStates.WaitForConfirmation;
					$(buttonCancel).css("display", "inline-block");
					if(textNode !== null)
					{
						textNode.textContent = "Generate new code";
					}
				}
				else
				{
					setTimeout = options.model.EmailConfirmationState == ConfirmationStates.WaitForConfirmation;
					$(buttonOK).css("display", setTimeout ? "none" : "inline-block");
					$(buttonCancel).css("display", "none");
					if(textNode !== null)
					{
						textNode.textContent = "Send confirmation link";
					}
				}
				if(setTimeout && typeof this._mainTimeoutID !== "number")
				{
					this._mainTimeoutID = window.setTimeout(function() { that._mainTimeoutExpiredCallback(); }, registrationLockoutInterval * 1000);
					/*
					if(options.dialogType === ConfirmDialogTypes.ConfirmEmail)
					{
						//this._checkTimeoutID = window.setTimeout(function() { that._checkTimeoutExpiredCallback(); }, 10 * 1000);
						//this._mainTimeoutID = window.setTimeout(function() { that.close(); }, 10 * 1000);
						this._mainTimeoutID = window.setTimeout(function()
						{
							window.location.href = `${window.location.origin}/UserAccount/CompleteConfirmEmail?${modelToQueryString(that.options.model)}`;
							return false;
						}, 10 * 1000);
					}
					*/
				}
			}
			catch(e)
			{
				console.error(e);
				throw e;
			}
		},
		_clearMainTimer: function()
		{
			if(typeof this._mainTimeoutID === "number")
			{
				clearTimeout(this._mainTimeoutID);
			}
		},
		_restartMainTimer: function()
		{
			this._clearMainTimer();
			_mainTimeoutID = setTimeout(function() { that._timeoutExpiredCallback(); }, registrationLockoutInterval * 1000);
		},
		_mainTimeoutExpiredCallback: function()
		{
			this._clearMainTimer();
			this._setErrorContentVisibility(true);
		},
		_checkTimeoutExpiredCallback: function()
		{
			if(typeof this._checkTimeoutID === "number")
			{
				clearTimeout(this._checkTimeoutID);
				_getEmailConfirmationState();
				if(this.model.EmailConfirmationState == ConfirmationStates.Complete)
				{
					this._clearMainTimer();
					window.location.href = `${window.location.origin}/UserAccount/Login?${modelToQueryString(that.options.model)}`;
					this.close();
					return false;
				}
				else
				{
					this._checkTimeoutID = window.setTimeout(function() { that._checkTimeoutExpiredCallback(); }, 10 * 1000);
				}
			}
		},
		_getEmailConfirmationState: function()
		{
			const that = this;
			const urlAction = window.location.origin + "/UserAccount/GetEmailConfirmationState";
			const jsonData = JSON.stringify(this.options.model);
			const jqXHR = $.ajax({
				method: "POST",
				url: urlAction,
				data: jsonData,
				dataType: "json",
				contentType: "application/json; charset=utf-8"
			});
			jqXHR?.done(function(response)
			{
				if(response?.confirmed === true)
				{
					that.model.EmailConfirmationState = ConfirmationStates.Complete;
				}
			});
			jqXHR?.fail(function()
			{
				alert(`request to ${urlAction} is failed`);
			});
		},
		_setErrorContentVisibility: function(timeoutExpired = false)
		{
			var defaultContentHidden = false;
			var errorContentHidden = true;
			if(this.options.dialogType == ConfirmDialogTypes.ConfirmPhoneNumber
				&& this.options.model.PhoneConfirmationState == ConfirmationStates.WaitForConfirmation && timeoutExpired
				|| this.options.dialogType == ConfirmDialogTypes.ConfirmEmail
				&& this.options.model.EmailConfirmationState == ConfirmationStates.WaitForConfirmation
				&& (timeoutExpired || $("#confirm-code-input").text() !== this.options.model.PhoneVerificationCode))
			{
				defaultContentHidden = true;
				errorContentHidden = false;
			}
			if($(".ui-confirm-dialog-default-content") != null && $(".ui-confirm-dialog-error-content") != null)
			{
				$(".ui-confirm-dialog-default-content").prop("hidden", defaultContentHidden);
				$(".ui-confirm-dialog-error-content").prop("hidden", errorContentHidden);
			}
		}
	});
}(jQuery));
