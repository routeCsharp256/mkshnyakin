﻿using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OzonEdu.MerchandiseService.Infrastructure.Contracts;

namespace OzonEdu.MerchandiseService.Infrastructure.Stubs
{
    public class OzonEduEmployeeServiceClient : IOzonEduEmployeeServiceClient
    {
        private static readonly IdentityGenerator IdGen = new();

        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        private static readonly ImmutableArray<EmployeeViewModel> Items = ImmutableArray.Create(new EmployeeViewModel[]
        {
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Антон",
                MiddleName = "Фёдорович",
                LastName = "Глушков",
                BirthDay = DateTime.Parse("08/18/1978", Culture),
                HiringDate = DateTime.Parse("01/01/2000", Culture),
                Email = "ololo1@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Даниил",
                MiddleName = "Иванович",
                LastName = "Гуляев",
                BirthDay = DateTime.Parse("03/15/1979", Culture),
                HiringDate = DateTime.Parse("02/02/2001", Culture),
                Email = "ololo2@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Максим",
                MiddleName = "Миронович",
                LastName = "Кузнецов",
                BirthDay = DateTime.Parse("04/16/1980", Culture),
                HiringDate = DateTime.Parse("03/03/2003", Culture),
                Email = "ololo3@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Тимур",
                MiddleName = "Николаевич",
                LastName = "Трофимов",
                BirthDay = DateTime.Parse("05/17/1981", Culture),
                HiringDate = DateTime.Parse("04/04/2004", Culture),
                Email = "ololo4@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Мирослава",
                MiddleName = "Степановна",
                LastName = "Смирнова",
                BirthDay = DateTime.Parse("06/18/1982", Culture),
                HiringDate = DateTime.Parse("05/05/2005", Culture),
                Email = "ololo5@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Иван",
                MiddleName = "Георгиевич",
                LastName = "Захаров",
                BirthDay = DateTime.Parse("07/19/1983", Culture),
                HiringDate = DateTime.Parse("06/06/2006", Culture),
                Email = "ololo6@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Виктория",
                MiddleName = "Никитична",
                LastName = "Колесникова",
                BirthDay = DateTime.Parse("08/20/1984", Culture),
                HiringDate = DateTime.Parse("07/07/2007", Culture),
                Email = "ololo7@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Ева",
                MiddleName = "Фёдоровна",
                LastName = "Никитина",
                BirthDay = DateTime.Parse("09/21/1985", Culture),
                HiringDate = DateTime.Parse("08/08/2008", Culture),
                Email = "ololo8@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Марк",
                MiddleName = "Кириллович",
                LastName = "Куликов",
                BirthDay = DateTime.Parse("10/22/1986", Culture),
                HiringDate = DateTime.Parse("09/09/2009", Culture),
                Email = "ololo9@example.com"
            },
            new()
            {
                Id = IdGen.Get(),
                FirstName = "Мария",
                MiddleName = "Никитична",
                LastName = "Быкова",
                BirthDay = DateTime.Parse("11/23/1987", Culture),
                HiringDate = DateTime.Parse("10/10/2010", Culture),
                Email = "ololo10@example.com"
            }
        });

        public Task<EmployeeViewModel> GetByIdAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            var result = Items.FirstOrDefault(x => x.Id == employeeId);
            return Task.FromResult(result);
        }

        public Task<EmployeeViewModel> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var result = Items.FirstOrDefault(x => x.Email == email);
            return Task.FromResult(result);
        }

        public class EmployeeViewModel
        {
            public long Id { get; init; }
            public string FirstName { get; init; }
            public string LastName { get; init; }
            public string MiddleName { get; init; }
            public DateTime BirthDay { get; init; }
            public DateTime HiringDate { get; init; }
            public string Email { get; init; }
        }
    }
}