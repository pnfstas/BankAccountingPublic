class UserAccountForm extends HTMLFormElement
{
	constructor()
	{
		this.super();
		const that = this;
		$(this).on("submit", function(event)
		{
			that.submitted(true);
		});
		$(this).on("invalid", function(event)
		{
			that.submitted(false);
		});
	}
	get submitted() { return this._internals.states.has("--submitted") }
	set submitted(value)
	{
		if(value)
		{
			this._internals.states.add("--submitted");
		}
		else
		{
			this._internals.states.remove("--submitted");
		}
	}
}
$(function()
{
	const curDefinedElement = customElements.get("account-form");
	if(typeof curDefinedElement === "undefined" || !(curDefinedElement instanceof UserAccountForm))
	{
		customElements.define("account-form", UserAccountForm);
	}
});