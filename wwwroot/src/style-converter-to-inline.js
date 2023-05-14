class StyleConverter
{
	static applyStyle(element, includePseudoElt = false)
	{
		var result = false; 
		try
		{
			if(ObjectExtensions.hasBasePrototype(element, "Element"))
			{
				for(const childElement of element.children)
				{
					StyleConverter.applyStyle(childElement, includePseudoElt);
				}
				const mainStyle = getComputedStyle(element);
				if(mainStyle != null)
				{
					var rules = null;
					var hoverStyle = null;
					if(includePseudoElt)
					{
						const regex = new RegExp(`${element.tagName.toLowerCase()}(\\.[A-Za-z0-9-_]+|\\#[A-Za-z0-9-_]+)*(\\[hover\]|(\\[[^\\]]+\\])*(\\:(has|is)\\()*\\:hover)`);
						rules = Array.from(document.styleSheets[0].cssRules)?.filter(currule => currule?.selectorText?.search(regex) >= 0);
						hoverStyle = getComputedStyle(element, ":hover");
					}
					if(rules?.length > 0 && hoverStyle != null)
					{
						const inlineStyle = {};
						const hoverInlineStyle = {};
						for(var index = 0; index < mainStyle.length; index++)
						{
							const property = mainStyle[index];
							const value = mainStyle.getPropertyValue(property);
							if(ObjectExtensions.isNotEmptyString(value))
							{
								inlineStyle[property] = value;
							}
						}
						for(var index = 0; index < hoverStyle.length; index++)
						{
							const property = hoverStyle[index];
							const value = hoverStyle.getPropertyValue(property);
							if(ObjectExtensions.isNotEmptyString(value))
							{
								hoverInlineStyle[property] = value;
							}
						}
						if(Object.keys(hoverInlineStyle)?.length > 0)
						{
							var strInlineStyle = JSON.stringify(inlineStyle, null, " ")?.replace(", ", "");
							if(ObjectExtensions.isNotEmptyString(strInlineStyle))
							{
								const strHoverInlineStyle = JSON.stringify(hoverInlineStyle, null, " ")?.replace(", ", "");
								if(ObjectExtensions.isNotEmptyString(strHoverInlineStyle))
								{
									strInlineStyle += ":hover " + strHoverInlineStyle;
								}
								element.setAttribute("style", strInlineStyle);
								//element.style = strInlineStyle;
							}
						}
					}
					else
					{
						for(var index = 0; index < mainStyle.length; index++)
						{
							const property = mainStyle[index];
							const value = mainStyle.getPropertyValue(property);
							if(ObjectExtensions.isNotEmptyString(value))
							{
								element.style[property] = value;
							}
						}
					}
				}
				result = true;
			}
		}
		catch(e)
		{
			consol.error(e);
			throw e;
		}
		return result;
	}
}
