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
using COMCMS.Common;
using System.Runtime.InteropServices;

namespace COMCMS.Core
{
    /// <summary>Tag标签</summary>
    public partial class Tags : Entity<Tags>
    {
        #region 对象操作
        static Tags()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(Counts));

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
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化Tags[Tag标签]数据……");

        //    var entity = new Tags();
        //    entity.Id = 0;
        //    entity.TagName = "abc";
        //    entity.Keywords = "abc";
        //    entity.Description = "abc";
        //    entity.Counts = 0;
        //    entity.Hits = 0;
        //    entity.IsTop = 0;
        //    entity.AddTime = DateTime.Now;
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化Tags[Tag标签]数据！");
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
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static Tags FindById(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.Id == id);
        }

        /// <summary>根据Tag标签名称查找</summary>
        /// <param name="tagName">Tag标签名称</param>
        /// <returns>实体列表</returns>
        public static IList<Tags> FindAllByTagName(String tagName)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.TagName == tagName);

            return FindAll(_.TagName == tagName);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="tagName">Tag标签名称</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<Tags> Search(String tagName, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (!tagName.IsNullOrEmpty()) exp &= _.TagName == tagName;
            if (!key.IsNullOrEmpty()) exp &= _.Keywords.Contains(key) | _.Description.Contains(key);

            return FindAll(exp, page);
        }

        // Select Count(Id) as Id,TagName From Tags Where CreateTime>'2020-01-24 00:00:00' Group By TagName Order By Id Desc limit 20
        static readonly FieldCache<Tags> _TagNameCache = new FieldCache<Tags>(nameof(TagName))
        {
            //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        };

        /// <summary>获取Tag标签名称列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
        /// <returns></returns>
        public static IDictionary<String, String> GetTagNameList() => _TagNameCache.FindAllName();
        #endregion

        #region 业务操作
        /// <summary>
        /// 添加新的TAG标签
        /// </summary>
        /// <param name="listtag">标签，原始，用“,”隔开的字符串</param>
        /// <param name="xtypeid">类型</param>
        /// <param name="rid">目标ID，文章ID，商品ID等...</param>
        /// <param name="tagtitle">目标标题，文章标题，商品标题等（缓存用的）</param>
        public static void InsertTags(string listtag, Utils.CMSType xtypeid, int rid, string tagtitle)
        {
            //流程：分割TAG，遍历标签；判断是否存在；插入。
            //1、分割标签
            if (!string.IsNullOrEmpty(listtag))
            {
                string[] arrtags = listtag.Split(new string[] { "," }, StringSplitOptions.None);
                if (arrtags != null && arrtags.Length > 0)
                {
                    foreach (string s in arrtags)
                    {
                        //2、单个标签处理
                        if (!string.IsNullOrEmpty(s) && Utils.GetStringLength(s.Trim()) < 100)
                        {
                            TagsDetail tagsDetail = new TagsDetail();
                            Tags tmptag = new Tags();

                            //判断是否存在这个标签
                            if (FindCount(_.TagName == s.Trim(), null, null, 0, 0) > 0)
                            {
                                tmptag = Find(_.TagName == s.Trim()); //GetModel(s.Trim());
                                tmptag.Counts = tmptag.Counts + 1;
                                tmptag.Update();
                            }
                            else
                            {
                                tmptag = new Tags()
                                {
                                    AddTime = DateTime.Now,
                                    Counts = 1,
                                    Description = "",
                                    Hits = 0,
                                    IsTop = 0,
                                    Keywords = "",
                                    TagName = s.Trim(),
                                };
                                tmptag.Insert();
                            }
                            tagsDetail.RId = rid;
                            tagsDetail.TypeId = xtypeid.ToInt();
                            tagsDetail.TagName = s.Trim();
                            tagsDetail.TagsId = tmptag.Id;
                            tagsDetail.Title = tagtitle;
                            tagsDetail.Hits = 0;
                            tagsDetail.AddTime = DateTime.Now;
                            tagsDetail.Insert();
                        }
                    }
  
                }
            }
        }

        #region 编辑标签
        /// <summary>
        /// 编辑Tag标签
        /// </summary>
        /// <param name="listtag">标签，原始，用“,”隔开的字符串</param>
        /// <param name="xtypeid">类型</param>
        /// <param name="rid">目标ID，文章ID，商品ID等...</param>
        /// <param name="tagtitle">目标标题，文章标题，商品标题等（缓存用的）</param>
        public static void ModifyTags(string listtag, Utils.CMSType xtypeid, int rid, string tagtitle)
        {
            IList<TagsDetail> listdetail = TagsDetail.FindAll(TagsDetail._.RId == rid & TagsDetail._.TypeId == (int)xtypeid, null, null, 0, 0);
            if (!string.IsNullOrEmpty(listtag))
            {
                string[] arrtags = listtag.Split(new string[] { "," }, StringSplitOptions.None);
                if (arrtags != null && arrtags.Length > 0)
                {
                    foreach (string s in arrtags)
                    {
                        //2、单个标签处理 只有 添加或者 删除。没有修改的
                        if (!string.IsNullOrEmpty(s) && Utils.GetStringLength(s.Trim()) < 100)
                        {
                            //判断是否存在
                            var td = listdetail.Find(x => x.TagName == s.Trim());
                            if(td == null)
                            {
                                TagsDetail tagsDetail = new TagsDetail();
                                Tags tmptag = new Tags();

                                //判断是否存在这个标签
                                if (FindCount(_.TagName == s.Trim(), null, null, 0, 0) > 0)
                                {
                                    tmptag = Find(_.TagName == s.Trim()); //GetModel(s.Trim());
                                    tmptag.Counts = tmptag.Counts + 1;
                                    tmptag.Update();
                                }
                                else
                                {
                                    tmptag = new Tags()
                                    {
                                        AddTime = DateTime.Now,
                                        Counts = 1,
                                        Description = "",
                                        Hits = 0,
                                        IsTop = 0,
                                        Keywords = "",
                                        TagName = s.Trim(),
                                    };
                                    tmptag.Insert();
                                }
                                tagsDetail.RId = rid;
                                tagsDetail.TypeId = xtypeid.ToInt();
                                tagsDetail.TagName = s.Trim();
                                tagsDetail.TagsId = tmptag.Id;
                                tagsDetail.Title = tagtitle;
                                tagsDetail.Hits = 0;
                                tagsDetail.AddTime = DateTime.Now;
                                tagsDetail.Insert();
                            }
                            else
                            {
                                listdetail.Remove(td);

                                var tmptag = Find(_.TagName == s.Trim());
                                if(tmptag != null)
                                {
                                    long counts = TagsDetail.FindCount(TagsDetail._.TagName == s.Trim(), null, null, 0, 0);
                                    if (counts == 0)
                                        tmptag.Delete();
                                    else
                                    {
                                        tmptag.Counts = (int)counts;
                                        tmptag.Update();
                                    }
                                }
                                


                            }
                        }
                    }

                }
            }
        }
        #endregion

        #region 删除标签
        /// <summary>
        /// 删除标签
        /// </summary>
        /// <param name="listtag">标签集合</param>
        public static void DeleteTag(string listtag, Utils.CMSType xtypeid, int rid)
        {
            if (!string.IsNullOrEmpty(listtag))
            {
                
                string[] arrtags = listtag.Split(new string[] { "," }, StringSplitOptions.None);
                if (arrtags != null && arrtags.Length > 0)
                {
                    foreach (var s in arrtags)
                    {
                        var tmp = TagsDetail.Find(TagsDetail._.TypeId == xtypeid.ToInt() & TagsDetail._.TagName == s.Trim() & TagsDetail._.RId == rid);
                        if (tmp != null)
                        {
                            tmp.Delete();
                        }
                    }
                }
            }
        }
        #endregion
        #endregion
    }
}