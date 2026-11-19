using System.Collections.Generic;
using BusinessEntities;
using Common;
using Data.Repositories;

namespace Core.Services.Users
{
    [AutoRegister]
    public class UpdateUserService : IUpdateUserService
    {
        private readonly IUserRepository _userRepository;

        public UpdateUserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public void Update(User user, string name, string email, UserTypes type, decimal? annualSalary, IEnumerable<string> tags)
        {
            user.SetEmail(email);
            user.SetName(name);
            user.SetType(type);
            user.SetMonthlySalary(annualSalary.Value / 12);
            user.SetTags(tags);
            _userRepository.Save(user);
        }
    }
}