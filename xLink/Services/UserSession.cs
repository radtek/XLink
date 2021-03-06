﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Model;
using NewLife.Net;
using NewLife.Remoting;
using NewLife.Threading;
using xLink.Entity;
using xLink.Models;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace xLink.Services
{
    /// <summary>用户会话</summary>
    [DisplayName("用户")]
    public class UserSession : LinkSession
    {
        #region 属性
        #endregion

        #region 构造
        static UserSession()
        {
            // 异步初始化数据
            ThreadPoolX.QueueUserWorkItem(() =>
            {
                var n = 0;
                n = User.Meta.Count;
                n = UserOnline.Meta.Count;
                n = UserHistory.Meta.Count;
            });
        }

        /// <summary>实例化</summary>
        public UserSession()
        {
            //GetData = OnGetData;
            //SetData = OnSetData;
        }
        #endregion

        #region 登录
        /// <summary>查找用户并登录，找不到用户是返回空，登录失败则抛出异常</summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="ps">附加参数</param>
        /// <returns></returns>
        protected override IManageUser CheckUser(String user, String pass, IDictionary<String, Object> ps)
        {
            var u = User.FindByName(user);
            if (u == null)
            {
                u = new User
                {
                    Name = user,
                    Password = pass.MD5(),
                    Enable = true
                };
                u.SaveRegister(Session as INetSession);
            }

            // 登录
            Name = user;

            // 验证密码
            u.CheckMD5(pass);

            return u;
        }
        #endregion

        #region 操作历史
        /// <summary>创建在线</summary>
        /// <param name="sessionid"></param>
        /// <returns></returns>
        protected override IOnline CreateOnline(String sessionid)
        {
            var ns = Session as NetSession;

            var olt = UserOnline.FindBySessionID(sessionid) ?? new UserOnline();
            olt.Version = Version;
            olt.ExternalUri = ns.Remote + "";

            return olt;
        }

        /// <summary>创建历史</summary>
        /// <returns></returns>
        protected override IHistory CreateHistory()
        {
            var hi = new UserHistory
            {
                Version = Version,
                //NetType = NetType
            };

            return hi;
        }
        #endregion

        #region 读写
        /// <summary>收到写入请求</summary>
        /// <param name="id">设备</param>
        /// <param name="start"></param>
        /// <param name="data"></param>
        [Api("Write")]
        protected override DataModel OnWrite(String id, Int32 start, String data)
        {
            var err = "";
            try
            {
                var dv = Device.FindByCode(id);
                if (dv == null) throw new ApiException(405, "找不到设备！");

                var ss = Session.AllSessions.FirstOrDefault(e => e["Current"] is DeviceSession d && d.Name.EqualIgnoreCase(id));
                if (ss == null) throw new Exception("设备离线");

                var ds = ss["Current"] as DeviceSession;
                var rs = ds.Write(id, start, data.ToHex()).Result;
            }
            catch (Exception ex)
            {
                err = ex?.GetTrue()?.Message;
                throw;
            }
            finally
            {
                SaveHistory("Write", err.IsNullOrEmpty(), "({0}, {1}, {2}) {3}".F(id, start, data.ToHex(), err));
            }

            return base.OnWrite(id, start, data);
        }

        /// <summary>收到读取请求</summary>
        /// <param name="id">设备</param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        [Api("Read")]
        protected override DataModel OnRead(String id, Int32 start, Int32 count)
        {
            var err = "";
            try
            {
                var ss = Session.AllSessions.FirstOrDefault(e => e["Current"] is DeviceSession d && d.Name.EqualIgnoreCase(id));
                if (ss is DeviceSession ds) TaskEx.Run(() => ds.Read(id, 0, 64));
            }
            catch (Exception ex)
            {
                err = ex?.GetTrue()?.Message;
                throw;
            }
            finally
            {
                SaveHistory("Read", err.IsNullOrEmpty(), "({0}, {1}, {2}) {3}".F(id, start, count, err));
            }

            return base.OnRead(id, start, count);
        }

        /// <summary>读取对方数据</summary>
        /// <param name="id">设备</param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override Task<Byte[]> Read(String id, Int32 start, Int32 count)
        {
            throw new NotSupportedException("不支持向用户端发起读取请求");
        }

        //private Byte[] OnGetData(String id)
        //{
        //    var dv = Device.FindByName(id);
        //    if (dv == null) throw new ApiException(405, "找不到设备！");

        //    return dv?.Data.ToHex();
        //}

        //private void OnSetData(String id, Byte[] data)
        //{
        //    var dv = Device.FindByName(id);
        //    if (dv == null) throw new ApiException(405, "找不到设备！");

        //    dv.Data = data.ToHex();
        //    dv.SaveAsync();
        //}
        #endregion
    }
}