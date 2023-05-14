function createModel()
{
	const model =
	{
		LoginType: LoginTypes.parse($("input[name='LoginType']:checked")?.val())
	};
	model.TwoFactorEnabled = model.LoginType != LoginTypes.LoginByUserName;
	$(".user-property-input").each(function(index)
	{
		const name = $(this).prop("name");
		model[name] = !$(this).is(":hidden") && ObjectExtensions.isNotEmptyString($(this).val()) ? $(this).val() : null;
	});
	model.PhoneNumber = ObjectExtensions.isNotEmptyString(model.DislayedPhoneNumber) ? model.DislayedPhoneNumber.match("/[0-9]+/")?.join("") : null;
	model.UserFullName = ObjectExtensions.isNotEmptyString(model.FirstName) ? model.FirstName + (ObjectExtensions.isNotEmptyString(model.LastName) ?
		` ${model.LastName}` : "") : model.LoginType != LoginTypes.LoginByUserName && ObjectExtensions.isNotEmptyString(model.Email) ?
		model.Email : model.LoginType != LoginTypes.LoginByUserName && ObjectExtensions.isNotEmptyString(model.PhoneNumber) ? model.PhoneNumber : model.UserName;
	return model;
}
function register()
{
	const model = createModel();
	if(typeof model?.LoginType === "number")
	{
		var urlAction = window.location.origin + "/UserAccount/GetModelValidationState";
		var jqXHR = $.ajax({
			method: "POST",
			url: urlAction
		});
		jqXHR?.done(function(response)
		{
			if(typeof response?.valid === "boolean")
			{
				if(model.Submitted = response.valid)
				{
					stylingSubmittedForm();
					window.location.href = `${window.location.origin}/UserAccount/ProcessRegister?${modelToQueryString(model)}`;
					return false;
				}
			}
		});
		jqXHR?.fail(function()
		{
			alert(`request to ${urlAction} is failed`);
		});
	}
}
const ConfirmDialogTypes =
{
	None: 0,
	ConfirmEmail: 1,
	ConfirmPhoneNumber: 2
};

var registrationLockoutInterval = 0;

$(async function()
{
	try
	{
		//$(".account-form.register").on("submit", register);
		if(model?.Submitted === true && model.hasOwnProperty("EmailConfirmationState") && model.hasOwnProperty("PhoneConfirmationState"))
		{
			const arrStates = [ConfirmationStates.VerificationSent, ConfirmationStates.WaitForConfirmation];
			if(model.EmailConfirmationState == ConfirmationStates.Complete || arrStates.includes(model.EmailConfirmationState)
				|| arrStates.includes(model.PhoneConfirmationState))
			{
				const module = await import(`./confirmdialog.js?v=${Date.now()}`);
				if(model.EmailConfirmationState == ConfirmationStates.Complete)
				{
					const dialogId = "#confirm-email-dialog";
					if($(dialogId).confirmdialog("isOpen"))
					{
						$(dialogId).confirmdialog("close");
					}
					window.location.href = `${window.location.origin}/UserAccount/Login?${modelToQueryString(model)}`;
					return false;
				}
				else
				{
					registrationLockoutInterval = $("body").data("registration-lockout-interval");
					const dialogId = arrStates.includes(model.PhoneConfirmationState) ? "#confirm-phone-dialog" : "#confirm-email-dialog";
					const dialogType = arrStates.includes(model.PhoneConfirmationState) ? ConfirmDialogTypes.ConfirmPhoneNumber : ConfirmDialogTypes.ConfirmEmail;
					$(dialogId).confirmdialog({ dialogType: dialogType, model: model });
					$(dialogId).confirmdialog("open");
				}
			}
			$("#email-confirm-state-span").css("color", model.EmailConfirmationState == ConfirmationStates.Complete ? "green" : "red");
			$("#phone-confirm-state-span").css("color", model.PhoneConfirmationState == ConfirmationStates.Complete ? "green" : "red");
		}
	}
	catch(e)
	{
		console.error(e);
		throw e;
	}
});
