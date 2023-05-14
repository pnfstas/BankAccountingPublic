class LoginTypes
{
	static LoginByUserName = 0;
	static LoginByEmail = 1;
	static LoginByPhoneNumber = 2;
	static parse(strValue)
	{
		var result = LoginTypes.LoginByUserName;
		switch(strValue?.trim())
		{
			case "LoginByUserName":
				result = LoginTypes.LoginByUserName;
				break;
			case "LoginByEmail":
				result = LoginTypes.LoginByEmail;
				break;
			case "LoginByPhoneNumber":
				result = LoginTypes.LoginByPhoneNumber;
				break;
		}
		return result;
	}
};
class ConfirmationStates
{
	static NotStarted = 0;
	static VerificationSent = 1;
	static WaitForConfirmation = 2;
	static Complete = 3;
	static parse(strValue)
	{
		var result = ConfirmationStates.NotStarted;
		switch(strValue?.trim())
		{
			case "VerificationSent":
				result = ConfirmationStates.VerificationSent;
				break;
			case "WaitForConfirmation":
				result = ConfirmationStates.WaitForConfirmation;
				break;
			case "Complete":
				result = ConfirmationStates.Complete;
				break;
		}
		return result;
	}
};
function getModelFromRequest()
{
	var model = null;
	const params = new URLSearchParams(window.location.search);
	if(params !== null)
	{
		if(params.has("model"))
		{
			const strModel = params.get("model");
			model = ObjectExtensions.isNotEmptyString(strModel) ? JSON.parse(strModel) : null;
		}
		else
		{
			const entries = Array.from(params.entries())?.filter(([key, value]) => ObjectExtensions.isNotEmptyString(key));
			if(entries?.length > 0)
			{
				model = {};
				for(const [k, v] of entries)
				{
					const key = k.trim();
					const value = v.trim();
					switch(key)
					{
						case "LoginType":
							model.LoginType = LoginTypes.parse(value);
							break;
						case "EmailConfirmationState":
						case "PhoneConfirmationState":
							model[key] = ConfirmationStates.parse(value);
							break;
						case "TwoFactorEnabled":
						case "RememberMe":
						case "Submitted":
							model[key] = value.toLowerCase() === "true";
							break;
						default:
							model[key] = ObjectExtensions.isNotEmptyString(value) ? value : null;
							break;
					}
					
				}
			}
		}
	}
	return model;
}
function modelToQueryString(model)
{
	const entries = Object.entries(model)?.filter(([key, value]) => ObjectExtensions.isNotEmptyString(key) && (typeof value === "boolean"
		|| typeof value === "number" && value !== NaN || ObjectExtensions.isNotEmptyString(value)));
	return new URLSearchParams(entries);
}
function validateInputElement(element, errorMessages)
{
	const name = $(element).prop("name");
	element.setCustomValidity("");
	if(!element.checkValidity() || (name === "PasswordConfirmation" && $(element).val() !== $("#Password").val())
		&& ObjectExtensions.isNotEmptyString(errorMessages[name]))
	{
		element.setCustomValidity(errorMessages[name]);
	}
	element.reportValidity();
}
function stylingSubmittedForm()
{
	if(!$(".account-form").hasClass("submitted"))
	{
		$(".account-form").addClass("submitted");
	}
	$(".account-form input").each(function(index)
	{
		$(this).prop("disabled", true);
	});
}

var model = null;

$(function()
{
	try
	{
		model = getModelFromRequest();
		var handler = null;
		const loginTypeData =
		{
			"login-by-username-radio": { loginType: LoginTypes.LoginByUserName, inputId: "UserName" },
			"login-by-email-radio": { loginType: LoginTypes.LoginByEmail, inputId: "Email" },
			"login-by-phone-radio": { loginType: LoginTypes.LoginByPhoneNumber, inputId: "DislayedPhoneNumber" }
		};
		const errorMessages = JSON.parse($("body").attr("data-error-messages"));
		const isNotErrorMessagesEmpty = typeof errorMessages === "object" && Object.keys(errorMessages)?.length > 0;
		if(model?.Submitted === true)
		{
			stylingSubmittedForm();
		}
		else
		{
			if(isNotErrorMessagesEmpty)
			{
				$(".user-property-input").each(function(index)
				{
					validateInputElement(this, errorMessages);
					handler = $(this).data("oninput");
					if(typeof handler !== "function")
					{
						$(this).on("input", handler = function(event)
						{
							validateInputElement(this, errorMessages);
						});
						$(this).data("oninput", handler);
					}
					handler = $(this).data("onmouseover");
					if(typeof handler !== "function")
					{
						$(this).on("mouseover", handler = function(event)
						{
							validateInputElement(this, errorMessages);
						});
						$(this).data("onmouseover", handler);
					}
				});
			}
		}
		$(".login-type-radio").each(function(index)
		{
			handler = $(this).data("onchange");
			if(typeof handler !== "function")
			{
				$(this).on("change", handler = function(event)
				{
					const radio = this;
					$(".login-input").each(function(index)
					{
						const isDisplayed = this.id === loginTypeData[radio.id].inputId;
						if(model?.Submitted !== true && isNotErrorMessagesEmpty)
						{
							$(this).prop("required", isDisplayed);
							validateInputElement(this, errorMessages);
						}
						if(isDisplayed)
						{
							$(this).parent()?.css(
							{
								"flex": "initial",
								"display": "flex",
								"flex-direction": "column",
								"align-items": "stretch",
								"justify-content": "normal"
							});
						}
						else
						{
							$(this).parent()?.css("display", "none");
						}
					});
				});
				$(this).data("onchange", handler);
			}
			if(typeof model?.LoginType === "number")
			{
				$(this).prop("checked", loginTypeData[this.id].loginType === model.LoginType);
			}
			else
			{
				$(this).prop("checked", loginTypeData[this.id].loginType == LoginTypes.LoginByUserName);
			}
			if($(this).prop("checked"))
			{
				$(this).trigger("change");
			}
		});
	}
	catch(e)
	{
		console.error(e);
		throw e;
	}
});
