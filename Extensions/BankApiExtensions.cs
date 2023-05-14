using BankAccountingApi.Helpers;
using BankAccountingApi.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace BankAccountingApi.Extensions
{
    public static class UserManagerExtensions
    {
        public static BankApiUser FindByPhoneNumber(this UserManager<BankApiUser> userManager, string strPhoneNumber)
        {
            return userManager?.Users?.ToList<BankApiUser>()?.FirstOrDefault<BankApiUser>(curuser => string.Equals(curuser?.PhoneNumber, strPhoneNumber));
        }
        public static string GenerateNewUserName(this UserManager<BankApiUser> userManager)
        {
            string strName = SecurityHelper.GenerateRandomString(BankAccountingApi.Startup.Startup.UserOptions.DefaultUserNameLength);
            return Regex.Replace(strName, "[^A-Za-z0-9_\\+-\\.\\@]", "");
        }
    }
   
    public static class ControllerExtensions
    {
        public static async Task<string> RenderViewAsString(this Controller controller, string? strViewName = null)
        {
            string strViewContent = null;
            if(controller?.ControllerContext != null && controller.HttpContext != null && controller.ViewData.Model != null)
            {
                if(string.IsNullOrWhiteSpace(strViewName))
                {
                    strViewName = controller.ControllerContext.ActionDescriptor?.ActionName;
                }
                IRazorViewEngine viewEngine = controller.HttpContext.RequestServices?.GetService(typeof(IRazorViewEngine)) as IRazorViewEngine;
                IView view = viewEngine.FindView(controller.ControllerContext, strViewName, true)?.View;
                if(view != null)
                {
                    StringWriter writer = new StringWriter();
                    ViewContext viewContext = new ViewContext(controller.ControllerContext, view, controller.ViewData, controller.TempData, writer, new HtmlHelperOptions());
                    await view.RenderAsync(viewContext);
                    strViewContent = writer.ToString();
                }
            }
            return strViewContent;
        }
    }

    public static class TypeExtensions
    {
        public static string GetMemberDescription(this Type type, string strMemberName)
        {
            MemberInfo memberInfo =
                (from curMemberInfo in type?.GetMember(strMemberName)
                 where string.Equals(curMemberInfo?.Name, strMemberName)
                 select curMemberInfo).FirstOrDefault();
            string description = null;
            try
            {
                description =
                    (from curConstructorArguments in
                    (from curCustomAttributeData in CustomAttributeData.GetCustomAttributes(memberInfo)
                     where curCustomAttributeData?.AttributeType == typeof(DescriptionAttribute)
                         && curCustomAttributeData.ConstructorArguments?.Count > 0
                     select curCustomAttributeData.ConstructorArguments).FirstOrDefault()
                     select curConstructorArguments.Value as string).FirstOrDefault();
            }
            catch(Exception e)
            {
                Debug.WriteLine($"{e}");
                throw e;
            }
            return description;
        }
    }

    public static class EnumExtensions
    {
        public static string GetValueDescription(this Enum value)
        {
            Type type = value?.GetType();
            return type?.GetMemberDescription(Enum.GetName(type, value));
        }
    }

    public static class ObjectExtensions
    {
        public static object TryCast(this object source, Type targetType)
        {
            object result = null;
            MethodInfo genericMethodInfo = typeof(ObjectExtensions).GetMethod("TryCast", 1, BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(object) }, null);
            if(genericMethodInfo != null)
            {
                MethodInfo methodInfo = genericMethodInfo.MakeGenericMethod(new Type[] { targetType });
                result = methodInfo?.Invoke(null, new object[] { source });
            }
            return result;
        }
        public static T TryCast<T>(this object source)
        {
            T result = default;
            Type typeSource = source?.GetType();
            if(typeof(T).IsAssignableFrom(typeSource))
            {
                result = (T)source;
            }
            else
            {
                string[] arrNames = null;
                Array arrValues = null;
                int index = 0;
                if(typeof(T).IsEnum && typeSource == typeof(string))
                {
                    arrNames = Enum.GetNames(typeof(T));
                    arrValues = Enum.GetValues(typeof(T));
                    index = Array.IndexOf(arrNames, source as string);
                    if(index >= 0)
                    {
                        result = (T)arrValues.GetValue(index);
                    }
                }
                else if(typeof(T) == typeof(string) && typeSource.IsEnum)
                {
                    result = (T)(Enum.GetName(typeSource, source) as object);
                }
                else
                {
                    object[] arrSource = new object[] { source };
                    List<T> listCasted = null;
                    try
                    {
                        listCasted = arrSource.Cast<T>()?.ToList<T>();
                    }
                    finally
                    {
                        if(listCasted?.Count > 0)
                        {
                            result = listCasted[0];
                        }
                    }
                }
            }
            return result;
        }
        public static void CopyPropertiesFrom(this object target, object source)
        {
            if(target != null && source != null)
            {
                Dictionary<string, PropertyInfo> sourceProperties = new Dictionary<string, PropertyInfo>(
                    from CurPropInfo in source.GetType()?.GetProperties()
                    where CurPropInfo.GetMethod.IsPublic && !CurPropInfo.GetMethod.IsAbstract
                    select new KeyValuePair<string, PropertyInfo>(CurPropInfo.Name, CurPropInfo));
                IEnumerable<PropertyInfo> targetProperties = target.GetType()?.GetProperties()?
                    .Where<PropertyInfo>(targetPropInfo => targetPropInfo?.SetMethod?.IsPublic == true && !targetPropInfo.SetMethod.IsAbstract
                        && sourceProperties?.ContainsKey(targetPropInfo.Name) == true);
                if(targetProperties != null)
                {
                    object curSource = null;
                    object curTarget = null;
                    object curValue = null;
                    foreach(PropertyInfo propInfo in targetProperties)
                    {
                        curSource = sourceProperties[propInfo.Name].GetMethod.IsStatic ? null : source;
                        curTarget = propInfo.SetMethod.IsStatic ? null : target;
                        curValue = sourceProperties[propInfo.Name].GetValue(curSource);
                        if(propInfo.PropertyType != sourceProperties[propInfo.Name].PropertyType)
                        {
                            curValue = curValue?.TryCast(propInfo.PropertyType);
                        }
                        propInfo.SetValue(curTarget, curValue);
                    }
                }
            }
        }
        public static string ToQueryString(this object obj)
        {
            JObject jobj = null;
            try
            {
                jobj = JObject.FromObject(obj);
            }
            catch(Exception e)
            {
                Debug.WriteLine($"{e}");
                throw e;
            }
            string strQueryString = string.Join<string>('&',
                from curProperty in jobj?.Properties()
                let strValue = curProperty?.Value?.ToString()
                where !string.IsNullOrWhiteSpace(curProperty?.Name) && !string.IsNullOrWhiteSpace(strValue)
                select $"{curProperty.Name}={HttpUtility.UrlEncode(strValue)}");
            return strQueryString;
        }
    }

    namespace HtmlAgilityPack
    {
        public static class HtmlNodeExtensions
        {
            public static IEnumerable<HtmlNode> AllElements(this HtmlNode node)
            {
                return node?.ChildNodes?.Where<HtmlNode>(curNode => curNode?.NodeType == HtmlNodeType.Element);
            }
        }
    }
}
