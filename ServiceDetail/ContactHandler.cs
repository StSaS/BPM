using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;

namespace ServiceDetail
{
    public class ContactHandler
    {
        private UserConnection _userConnection;

        public ContactHandler(UserConnection uc)
        {
            _userConnection = uc;
        }
        public string CopyESM(Guid contactId)
        {
            var manager = _userConnection.EntitySchemaManager.GetInstanceByName("Contact");

            var contact = manager.CreateEntity(_userConnection);

            bool result = contact.FetchFromDB(contactId); // where Id == contactId 

            if (result)
            {
                var Name = contact.GetColumnValue("Name");
                var AccountId = contact.GetColumnValue("AccountId");
                var Gender = contact.GetColumnValue("GenderId");
                var Job = contact.GetColumnValue("JobId");
                var JobT = contact.GetColumnValue("JobTitle");
                var DR = contact.GetColumnValue("DecisionRoleId");
                var email = contact.GetColumnValue("Email");
                var phone = contact.GetColumnValue("Phone");
                var address = contact.GetColumnValue("Address");

                

                var contactInsert = manager.CreateEntity(_userConnection);
                var guid = Guid.NewGuid();

                contactInsert.SetDefColumnValues();
                contactInsert.SetColumnValue("Name",Name);
                contactInsert.SetColumnValue("AccountId",AccountId);
                contactInsert.SetColumnValue("GenderId",Gender);
                contactInsert.SetColumnValue("JobId",Job);
                contactInsert.SetColumnValue("JobTitle",JobT);
                contactInsert.SetColumnValue("DecisionRoleId",DR);
                contactInsert.SetColumnValue("Email",email);
                contactInsert.SetColumnValue("Phone",phone);
                contactInsert.SetColumnValue("Address",address);
                contactInsert.SetColumnValue("Id", guid);

                contactInsert.Save();

                var esqResult = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "UsrFriend");
                esqResult.AddColumn("UsrContactFriend");
                esqResult.AddColumn("CreatedBy");
                esqResult.AddColumn("UsrContact");
                esqResult.AddColumn("UsrDescription");

                var ContactIdFilter = esqResult.CreateFilterWithParameters(FilterComparisonType.Equal,
                    "UsrContact", contactId);
                esqResult.Filters.Add(ContactIdFilter);

                var entities = esqResult.GetEntityCollection(_userConnection);

                

                for (int i = 0; i < entities.Count; i++)
                {
                    var uContactFriend = entities[i].GetColumnValue("UsrContactFriendId");
                    var uDescription = entities[i].GetColumnValue("UsrDescription");

                    var managerUsrFriend = _userConnection.EntitySchemaManager.GetInstanceByName("UsrFriend");

                    var usrFriend = managerUsrFriend.CreateEntity(_userConnection);

                    usrFriend.SetDefColumnValues();
                    usrFriend.SetDefColumnValue("UsrContactFriendId", uContactFriend);
                    usrFriend.SetDefColumnValue("UsrContactId", guid);
                    usrFriend.SetDefColumnValue("UsrDescription", uDescription);

                    usrFriend.Save();

                }

                return guid.ToString();
            }

            return "CopyESM";
        }
        public string Copy(Guid contactId)
        {
            
            //var result = "";
            //var id = Guid.NewGuid();

            var select = new Select(_userConnection)
                .Column("Name")
                .Column("AccountId")
                .Column("GenderId")
                .Column("JobId")
                .Column("JobTitle")
                .Column("DecisionRoleId")
                .Column("Email")
                .Column("Phone")
                .Column("Address")
                .From("Contact")
                .Where("Id").IsEqual(Column.Parameter(contactId)) as Select;
            var insel = new InsertSelect(_userConnection)
                .Into("Contact")
                //.Set("Id", Column.Parameter(id))
                .Set("Name", "AccountId","GenderId","JobId","JobTitle","DecisionRoleId","Email","Phone","Address")
                .FromSelect(select);

            var affectedRows = insel.Execute();

            //сортировать по дате, TOP(1) - c таким именем
            var sel = new Select(_userConnection)
                .Top(1)
                .Column("Id")
                .From("Contact")
                .OrderByDesc("CreatedOn")
                as Select;
             
            var newId = sel.ExecuteScalar<string>(); // id копии

            select = new Select(_userConnection)
                .Column("UsrContactFriendId")
                .Column("CreatedById")
                .Column("UsrDescription")
                .From("UsrFriend")
                .Where("UsrContactId").IsEqual(Column.Parameter(contactId)) as Select;//список для связки в детали

            using (DBExecutor dbExecutor = _userConnection.EnsureDBConnection())
            {
                using (IDataReader reader = select.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        var UsrContactFriendId = reader[0];
                        var CreatedById = reader[1];
                        var UsrDescript = reader[2];

                        var ins = new Insert(_userConnection)
                        .Into("UsrFriend")
                        .Set("UsrContactFriendId", Column.Parameter(UsrContactFriendId))
                        .Set("CreatedById", Column.Parameter(CreatedById))
                        .Set("UsrContactId", Column.Parameter(newId))
                        .Set("UsrDescription",Column.Parameter(UsrDescript));

                        affectedRows = ins.Execute();
                    }

                }
            }


            return newId;
        }


    }
}
