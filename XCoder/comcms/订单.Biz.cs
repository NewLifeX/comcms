using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Shards;

namespace COMCMS.Core
{
    /// <summary>订单</summary>
    public partial class Order : Entity<Order>
    {
        #region 对象操作
        static Order()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(UId));

            // 过滤器 UserModule、TimeModule、IPModule
        }

        /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);

            // 在新插入数据或者修改了指定字段时进行修正
            // 货币保留6位小数
            TotalPrice = Math.Round(TotalPrice, 6);
            Discount = Math.Round(Discount, 6);
            Fare = Math.Round(Fare, 6);
            TotalTax = Math.Round(TotalTax, 6);
            TotalPay = Math.Round(TotalPay, 6);
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化Order[订单]数据……");

        //    var entity = new Order();
        //    entity.OrderNum = "abc";
        //    entity.UId = 0;
        //    entity.UserName = "abc";
        //    entity.ShopId = 0;
        //    entity.Credit = 0;
        //    entity.TotalQty = 0;
        //    entity.TotalPrice = 0.0;
        //    entity.Discount = 0.0;
        //    entity.Fare = 0.0;
        //    entity.TotalTax = 0.0;
        //    entity.TotalPay = 0.0;
        //    entity.BackCredits = 0;
        //    entity.RealName = "abc";
        //    entity.Country = "abc";
        //    entity.Province = "abc";
        //    entity.City = "abc";
        //    entity.District = "abc";
        //    entity.Address = "abc";
        //    entity.PostCode = "abc";
        //    entity.Tel = "abc";
        //    entity.Mobile = "abc";
        //    entity.Email = "abc";
        //    entity.Notes = "abc";
        //    entity.AdminNotes = "abc";
        //    entity.Pic = "abc";
        //    entity.DeliverId = 0;
        //    entity.DeliverType = "abc";
        //    entity.DeliverNO = "abc";
        //    entity.DeliverNotes = "abc";
        //    entity.PayId = 0;
        //    entity.PayType = "abc";
        //    entity.PayTypeNotes = "abc";
        //    entity.OrderStatus = "abc";
        //    entity.PaymentStatus = "abc";
        //    entity.DeliverStatus = "abc";
        //    entity.AddTime = DateTime.Now;
        //    entity.Ip = "abc";
        //    entity.IsInvoice = 0;
        //    entity.InvoiceCompanyName = "abc";
        //    entity.InvoiceCompanyID = "abc";
        //    entity.InvoiceType = "abc";
        //    entity.InvoiceNote = "abc";
        //    entity.IsRead = 0;
        //    entity.IsEnd = 0;
        //    entity.EndTime = DateTime.Now;
        //    entity.IsOk = 0;
        //    entity.IsComment = 0;
        //    entity.Flag1 = "abc";
        //    entity.Flag2 = "abc";
        //    entity.Flag3 = "abc";
        //    entity.Title = "abc";
        //    entity.LastModTime = DateTime.Now;
        //    entity.OrderType = 0;
        //    entity.MyType = 0;
        //    entity.OutTradeNo = "abc";
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化Order[订单]数据！");
        //}

        ///// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        ///// <summary>已重载。在事务保护范围内处理业务，位于Valid之后</summary>
        ///// <returns></returns>
        //protected override Int32 OnDelete()
        //{
        //    return base.OnDelete();
        //}
        #endregion

        #region 扩展属性
        /// <summary>商户ID</summary>
        [XmlIgnore, IgnoreDataMember]
        //[ScriptIgnore]
        public Shop Shop => Extends.Get(nameof(Shop), k => Shop.FindById(ShopId));

        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static Order FindById(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.Id == id);
        }
        #endregion

        #region 高级查询

        // Select Count(Id) as Id,Category From Order Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By Id Desc limit 20
        //static readonly FieldCache<Order> _CategoryCache = new FieldCache<Order>(nameof(Category))
        //{
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        //};

        ///// <summary>获取类别列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
        ///// <returns></returns>
        //public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
        #endregion

        #region 业务操作
        #endregion
    }
}