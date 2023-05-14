using BankAccountingApi.Extensions;
using BankAccountingApi.Extensions.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BankAccountingApi.Models
{
    /*
    main menu:
        User profile    Settings    Account                 Pay cards
                                    (show all accounts)     (show all pay cards)
                                    СashDeposit             Open
                                    СashWithdrawal          Block
                                    Reports                 Close
                                        Balance             Reports
                                        Operations history      Balance
                                                                Operations history
    */
    public class MenuItemData
    {
        public string Href { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public bool IsFontIcon => Regex.IsMatch(Icon, "&#x[A-Z0-9]+");
        public bool HasItems { get => Items?.Count > 0; }
        public int ItemCount { get => Items?.Count ?? 0; }
        public List<MenuItemData> Items { get; set; } = new List<MenuItemData>();
        public MenuItemData GetSubItem(string title) => Items?.FirstOrDefault<MenuItemData>(curitem => string.Equals(curitem?.Title, title));
        public int GetSubItemIndex(string title) => Items?.IndexOf(GetSubItem(title)) ?? -1;
        public void SetSubItem(string title, MenuItemData value)
        {
            MenuItemData item = Items?.FirstOrDefault<MenuItemData>(curitem => string.Equals(curitem?.Title, title));
            if(item != null)
            {
                Items[Items.IndexOf(item)] = value;
            }
        }
        public MenuItemData(string? href = null, string? title = null, string? icon = null)
        {
            Href = href;
            Title = title;
            Icon = icon;
        }
    }
    public class HomeViewModel
    {
        public static HtmlDocument HtmlDocument { get; set; } = new HtmlDocument();
        public static int MainMenuItemCount { get; set; } = 4;
        public static List<MenuItemData> MainMenuItems { get; set; } = new List<MenuItemData>();
        public HomeViewModel()
        {
        }
        public static void FillMainMenuData(string strUserFullName)
        {
            MainMenuItems = new List<MenuItemData>()
            {
                new MenuItemData("#", "Account", "&#xE825"),
                new MenuItemData("#", "Pay cards", "&#xE8C7"),
                new MenuItemData("Home/Settings", "Settings", "&#xE115"),
                new MenuItemData("#", strUserFullName, "&#xE13D")
            };
            int index = -1;
            MainMenuItems[index = GetMainMenuItemIndex("Account")].Items = new List<MenuItemData>()
            {
                new MenuItemData("BankAccount/СashDeposit", "Сash deposit", "cash_deposit"),
                new MenuItemData("BankAccount/СashWithdrawals", "Сash withdrawals", "cash_withdrawals"),
                new MenuItemData("#", "Reports", "reports")
            };
            MainMenuItems[index].Items[2].Items = new List<MenuItemData>()
            {
                new MenuItemData("BankAccount/Balance", "Balance", "balance"),
                new MenuItemData("BankAccount/OperationsHistory", "Operations history", "account_history")
            };
            MainMenuItems[index = GetMainMenuItemIndex("Pay cards")].Items = new List<MenuItemData>()
            {
                new MenuItemData("PayCards/Open", "Open", "paycard_open"),
                new MenuItemData("PayCards/Block", "Block", "paycard_block"),
                new MenuItemData("PayCards/Close", "Close", "paycard_close"),
                new MenuItemData("#", "Reports", "reports")
            };
            MainMenuItems[index].Items[3].Items = new List<MenuItemData>()
            {
                new MenuItemData("PayCards/Balance", "Balance", "balance"),
                new MenuItemData("PayCards/OperationsHistory", "Operations history", "paycard_history")
            };
            MainMenuItems[index = GetMainMenuItemIndex(strUserFullName)].Items = new List<MenuItemData>()
            {
                new MenuItemData("UserAccount/EditProfile", "Edit profile", ""),
                new MenuItemData("UserAccount/LogOut", "Log out", ""),
            };
        }
        public static MenuItemData GetMainMenuItem(string title, MenuItemData? parent = null) =>
            (parent?.Items ?? MainMenuItems).FirstOrDefault<MenuItemData>(curitem => string.Equals(curitem?.Title, title));
        public static int GetMainMenuItemIndex(string title, MenuItemData? parent = null) => (parent?.Items ?? MainMenuItems).IndexOf(GetMainMenuItem(title, parent));
        public static string RenderMainMenuContent(string strUserFullName)
        {
            if(HomeViewModel.MainMenuItems.Count == 0)
            {
                HomeViewModel.FillMainMenuData(strUserFullName);
            }
            StringBuilder strBuilder = new StringBuilder();
            RenderMainMenuItems(strBuilder);
            return strBuilder.ToString();
        }
        public static void RenderMainMenuItems(StringBuilder strBuilder, int level = 0, MenuItemData menuItem = null)
        {
            string strClass = null;
            switch(level)
            {
            case 0:
                strClass = "main-menu-list";
                break;
            case 1:
                strClass = "main-submenu-list";
                break;
            default:
                strClass = "main-subsubmenu-list";
                break;
            }
            strBuilder.AppendLine($"<ul class=\"{strClass}\">");
            List<MenuItemData> menuItems = level == 0 ? HomeViewModel.MainMenuItems : menuItem.Items;
            foreach(MenuItemData curMenuItem in menuItems)
            {
                strBuilder.AppendLine("<li>");
                if(level == 0)
                {
                    strBuilder.AppendLine("<div>");
                }
                if(curMenuItem.IsFontIcon)
                {
                    strBuilder.AppendLine($"<span>{curMenuItem.Icon}</span>");
                }
                else
                {
                    strBuilder.AppendLine($"<img src=\"images/{curMenuItem.Icon}.png\" />");
                }
                strBuilder.AppendLine($"<a href=\"{curMenuItem.Href}\">{curMenuItem.Title}</a>");
                if(level == 0)
                {
                    strBuilder.AppendLine("</div>");
                }
                if(curMenuItem.HasItems)
                {
                    if(level > 0)
                    {
                        strBuilder.AppendLine($"<span>&#xE937</span>");
                    }
                    RenderMainMenuItems(strBuilder, level+1, curMenuItem);
                }
                strBuilder.AppendLine("</li>");
            }
            strBuilder.AppendLine("</ul>");
        }
        public static IHtmlContent RenderMainMenuHtml(string strUserFullName)
        {
            if(HomeViewModel.MainMenuItems.Count == 0)
            {
                HomeViewModel.FillMainMenuData(strUserFullName);
            }
            HtmlContentBuilder htmlBuilder = new HtmlContentBuilder();
            RenderMainMenuItems(htmlBuilder);
            return htmlBuilder;
        }
        public static void RenderMainMenuItems(HtmlContentBuilder htmlBuilder, int level = 0, MenuItemData menuItem = null)
        {
            string strClass = null;
            switch(level)
            {
            case 0:
                strClass = "main-menu-list";
                break;
            case 1:
                strClass = "main-submenu-list";
                break;
            default:
                strClass = "main-subsubmenu-list";
                break;
            }
            htmlBuilder.AppendLine($"<ul class=\"{strClass}\">");
            List<MenuItemData> menuItems = level == 0 ? HomeViewModel.MainMenuItems : menuItem.Items;
            foreach(MenuItemData curMenuItem in menuItems)
            {
                htmlBuilder.AppendLine("<li>");
                if(level == 0)
                {
                    htmlBuilder.AppendLine("<div>");
                }
                if(curMenuItem.IsFontIcon)
                {
                    htmlBuilder.AppendLine($"<span>{curMenuItem.Icon}</span>");
                }
                else
                {
                    htmlBuilder.AppendLine($"<img src=\"images/{curMenuItem.Icon}.png\" />");
                }
                htmlBuilder.AppendLine($"<a href=\"{curMenuItem.Href}\">{curMenuItem.Title}</a>");
                if(level == 0)
                {
                    htmlBuilder.AppendLine("</div>");
                }
                if(curMenuItem.HasItems)
                {
                    if(level > 0)
                    {
                        htmlBuilder.AppendLine($"<span>&#xE937</span>");
                    }
                    RenderMainMenuItems(htmlBuilder, level+1, curMenuItem);
                }
                htmlBuilder.AppendLine("</li>");
            }
            htmlBuilder.AppendLine("</ul>");
        }
        public static async Task InitMainMenu(Controller controller)
        {
            string strSignedInUser = null;
            if(controller.ViewData?.ContainsKey("SignedInUser") == true && !string.IsNullOrWhiteSpace(strSignedInUser = controller.ViewData["SignedInUser"] as string))
            {
                if(controller.ViewData.Model == null)
                {
                    controller.ViewData.Model = new HomeViewModel();
                }
                string strCurContent = await controller.RenderViewAsString();
                string strNewContent = InitMainMenu(strCurContent, strSignedInUser);
                await controller.HttpContext.Response.Body.WriteAsync(Encoding.Default.GetBytes(strNewContent), 0, strNewContent.Length);
            }
        }
        public static string InitMainMenu(string strContent, string strUserFullName)
        {
            string strNewContent = strContent;
            if(MainMenuItems.Count == 0)
            {
                HomeViewModel.FillMainMenuData(strUserFullName);
            }
            if(!string.IsNullOrWhiteSpace(strContent))
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(strContent);
                if(doc.ParseErrors.Count<HtmlParseError>() == 0)
                {
                    HtmlNode htmlElementMenu = doc.DocumentNode.SelectSingleNode("//nav[@id='main-menu']");
                    if(htmlElementMenu != null)
                    {
                        foreach(HtmlNode htmlElement in htmlElementMenu.AllElements())
                        {
                            htmlElementMenu.RemoveChild(htmlElement);
                        }
                        AddMainMenuItems(doc, htmlElementMenu);
                        strNewContent = doc.DocumentNode.WriteTo();
                    }
                }
            }
            return strNewContent;
        }
        public static void AddMainMenuItems(HtmlDocument doc, HtmlNode htmlParentElement, int level = 0, MenuItemData menuItem = null)
        {
            if(htmlParentElement.Elements("ul").Count<HtmlNode>() == 0)
            {
                HtmlNode htmlElementList = doc.CreateElement("ul");
                switch(level)
                {
                case 0:
                    htmlElementList.AddClass("main-menu-list");
                    break;
                case 1:
                    htmlElementList.AddClass("main-submenu-list");
                    break;
                default:
                    htmlElementList.AddClass("main-subsubmenu-list");
                    break;
                }
                htmlParentElement.AppendChild(htmlElementList);
                HtmlNode htmlListItem = null;
                HtmlNode htmlCurContainer = null;
                HtmlNode htmlDivElement = null;
                HtmlNode htmlSpanElement = null;
                HtmlNode htmlImgElement = null;
                HtmlNode htmlAnchorElement = null;
                List<MenuItemData> menuItems = level == 0 ? HomeViewModel.MainMenuItems : menuItem.Items;
                foreach(MenuItemData curMenuItem in menuItems)
                {
                    htmlListItem = doc.CreateElement("li");
                    htmlElementList.AppendChild(htmlListItem);
                    htmlCurContainer = htmlListItem;
                    if(level == 0)
                    {
                        htmlDivElement = doc.CreateElement("div");
                        htmlListItem.AppendChild(htmlDivElement);
                        htmlCurContainer = htmlDivElement;
                    }
                    if(curMenuItem.IsFontIcon)
                    {
                        htmlSpanElement = doc.CreateElement("span");
                        htmlSpanElement.InnerHtml = curMenuItem.Icon;
                        htmlCurContainer.AppendChild(htmlSpanElement);
                    }
                    else
                    {
                        htmlImgElement = doc.CreateElement("img");
                        htmlImgElement.SetAttributeValue("src", $"images/{curMenuItem.Icon}.png");
                        htmlCurContainer.AppendChild(htmlImgElement);
                    }
                    htmlAnchorElement = doc.CreateElement("a");
                    htmlAnchorElement.SetAttributeValue("href", curMenuItem.Href);
                    htmlAnchorElement.InnerHtml = curMenuItem.Title;
                    htmlCurContainer.AppendChild(htmlAnchorElement);
                    if(curMenuItem.HasItems)
                    {
                        if(level > 0)
                        {
                            htmlSpanElement = doc.CreateElement("span");
                            htmlSpanElement.InnerHtml = "&#xE937";
                            htmlListItem.AppendChild(htmlSpanElement);
                        }
                        AddMainMenuItems(doc, htmlListItem, level+1, curMenuItem);
                    }
                }
            }
        }
    }
}
