using System;
using System.Collections.Generic;
using BusinessEntities;

namespace Data.Repositories
{
    public interface IUserRepository
    {
        void Save(User user);
        void Delete(User user);
        User Get(Guid id);
        IEnumerable<User> Get(UserTypes? userType = null, string name = null, string email = null);
        IEnumerable<User> GetByTag(string tag);
        void DeleteAll();
    }
}