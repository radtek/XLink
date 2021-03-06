﻿using System;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube;
using NewLife.Web;
using ServerX = xLink.Master.Entity.Server;

namespace xLink.Master.Web.Controllers
{
    [Description("服务器信息，提供身份验证")]
    public class ServerController : EntityController<ServerX>
    {

        static ServerController()
        {
            var list = ListFields;
            list.RemoveField("Code")
                .RemoveField("LastLoginIP")
                .RemoveField("RegisterIP");
        }

        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override ActionResult IndexView(Pager p)
        {
            Boolean? flag = null;
            if (!p["enable"].IsNullOrEmpty()) flag = p["enable"].ToBoolean();

            var list = ServerX.Search(p["type"], flag, p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p["Q"], p);

            return View("List", list);
        }
    }
}