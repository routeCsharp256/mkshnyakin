﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using OpenTracing;
using OzonEdu.MerchandiseService.Domain.AggregationModels.MerchRequestAggregate;
using OzonEdu.MerchandiseService.Domain.Models;
using OzonEdu.MerchandiseService.Infrastructure.Repositories.Infrastructure.Interfaces;

namespace OzonEdu.MerchandiseService.Infrastructure.Repositories.Implementation
{
    public class MerchRequestPostgreSqlRepository : IMerchRequestRepository
    {
        private const int Timeout = 5;
        private readonly IChangeTracker _changeTracker;
        private readonly IDbConnectionFactory<NpgsqlConnection> _dbConnectionFactory;
        private readonly ITracer _tracer;

        public MerchRequestPostgreSqlRepository(
            IDbConnectionFactory<NpgsqlConnection> dbConnectionFactory,
            IChangeTracker changeTracker,
            ITracer tracer)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _changeTracker = changeTracker;
            _tracer = tracer;
        }

        public async Task<MerchRequest> CreateAsync(
            MerchRequest itemToCreate,
            CancellationToken cancellationToken = default)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(MerchRequestPostgreSqlRepository)}.{nameof(CreateAsync)}")
                .StartActive();

            const string sql = @"
                insert into merch_requests (employee_id, merch_type, status, mode, give_out_date, is_email_sended)
                values  (@EmployeeId, @MerchType, @Status, @Mode, @GiveOutDate, @IsEmailSended) returning id;
            ";

            var parameters = new
            {
                EmployeeId = itemToCreate.EmployeeId.Value,
                MerchType = itemToCreate.MerchType.Id,
                Status = itemToCreate.Status.Id,
                Mode = itemToCreate.Mode.Id,
                GiveOutDate = itemToCreate.GiveOutDate?.Value,
                IsEmailSended = itemToCreate.IsEmailSended
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var identity = await connection.QuerySingleOrDefaultAsync<long>(commandDefinition);
            if (identity != default)
            {
                itemToCreate.SetId(identity);
            }

            _changeTracker.Track(itemToCreate);
            return itemToCreate;
        }

        public async Task<MerchRequest> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(MerchRequestPostgreSqlRepository)}.{nameof(GetByIdAsync)}")
                .StartActive();

            const string sql = @"
                select id, employee_id, merch_type, status, mode, give_out_date, is_email_sended
                from merch_requests
                where id = @Id;
            ";

            var parameters = new
            {
                Id = id
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var merchRequestModel = await connection.QuerySingleOrDefaultAsync<Models.MerchRequest>(commandDefinition);
            var merchRequest = merchRequestModel is null
                ? null
                : CreateMerchRequestByModel(merchRequestModel);
            _changeTracker.Track(merchRequest);
            return merchRequest;
        }

        public async Task<MerchRequest> UpdateAsync(
            MerchRequest itemToUpdate,
            CancellationToken cancellationToken = default)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(MerchRequestPostgreSqlRepository)}.{nameof(UpdateAsync)}")
                .StartActive();

            const string sql = @"
                update merch_requests
                set
                    employee_id = @EmployeeId,
                    merch_type = @MerchType,
                    status = @Status,
                    mode = @Mode,
                    give_out_date = @GiveOutDate,
                    is_email_sended = @IsEmailSended
                where id = @Id;
            ";

            var parameters = new
            {
                Id = itemToUpdate.Id,
                EmployeeId = itemToUpdate.EmployeeId.Value,
                MerchType = itemToUpdate.MerchType.Id,
                Status = itemToUpdate.Status.Id,
                Mode = itemToUpdate.Mode.Id,
                GiveOutDate = itemToUpdate.GiveOutDate?.Value,
                IsEmailSended = itemToUpdate.IsEmailSended
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            await connection.ExecuteAsync(commandDefinition);
            _changeTracker.Track(itemToUpdate);
            return itemToUpdate;
        }

        public async Task<MerchRequest> DeleteAsync(
            MerchRequest itemToDelete,
            CancellationToken cancellationToken = default)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(MerchRequestPostgreSqlRepository)}.{nameof(DeleteAsync)}")
                .StartActive();

            const string sql = @"
                delete from merch_requests
                where id = @Id;
            ";

            var parameters = new
            {
                Id = itemToDelete.Id,
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            await connection.ExecuteAsync(commandDefinition);
            _changeTracker.Track(itemToDelete);
            return itemToDelete;
        }

        public async Task<IEnumerable<MerchRequest>> FindByEmployeeIdAsync(
            long employeeId,
            CancellationToken cancellationToken = default)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(MerchRequestPostgreSqlRepository)}.{nameof(FindByEmployeeIdAsync)}")
                .StartActive();

            const string sql = @"
                select id, employee_id, merch_type, status, mode, give_out_date, is_email_sended
                from merch_requests
                where employee_id = @EmployeeId;
            ";

            var parameters = new
            {
                EmployeeId = employeeId
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var queryResult = await QueryMerchRequestsAsync(connection, commandDefinition);

            var merchRequestItems = queryResult.ToArray();
            foreach (var merchRequest in merchRequestItems)
            {
                _changeTracker.Track(merchRequest);
            }

            return merchRequestItems;
        }

        public async Task<IEnumerable<MerchRequest>> FindCompletedByEmployeeIdAsync(
            long employeeId,
            CancellationToken cancellationToken = default)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(MerchRequestPostgreSqlRepository)}.{nameof(FindCompletedByEmployeeIdAsync)}")
                .StartActive();

            const string sql = @"
                select id, employee_id, merch_type, status, mode, give_out_date, is_email_sended
                from merch_requests
                where 
                    employee_id = @EmployeeId
                    and status = @Status;
            ";

            var parameters = new
            {
                EmployeeId = employeeId,
                Status = ProcessStatus.Complete.Id
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var queryResult = await QueryMerchRequestsAsync(connection, commandDefinition);

            var merchRequestItems = queryResult.ToArray();
            foreach (var merchRequest in merchRequestItems)
            {
                _changeTracker.Track(merchRequest);
            }

            return merchRequestItems;
        }

        public async Task<IEnumerable<MerchRequest>> FindOutOfStockByRequestMerchTypesAsync(
            IEnumerable<RequestMerchType> requestMerchTypes,
            CancellationToken cancellationToken = default)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(MerchRequestPostgreSqlRepository)}.{nameof(FindOutOfStockByRequestMerchTypesAsync)}")
                .StartActive();

            const string sql = @"
                select id, employee_id, merch_type, status, mode, give_out_date, is_email_sended
                from merch_requests
                where 
                    status = @Status
                    and merch_type = any(@MerchTypes);
            ";

            var parameters = new
            {
                Status = ProcessStatus.OutOfStock.Id,
                MerchTypes = requestMerchTypes.Select(x => x.Id).ToArray()
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var queryResult = await QueryMerchRequestsAsync(connection, commandDefinition);

            var merchRequestItems = queryResult.ToArray();
            foreach (var merchRequest in merchRequestItems)
            {
                _changeTracker.Track(merchRequest);
            }

            return merchRequestItems;
        }

        private static async Task<IEnumerable<MerchRequest>> QueryMerchRequestsAsync(
            NpgsqlConnection connection,
            CommandDefinition commandDefinition)
        {
            var merchRequestModels = await connection.QueryAsync<Models.MerchRequest>(commandDefinition);
            var merchRequestItems = merchRequestModels.Select(CreateMerchRequestByModel);
            return merchRequestItems;
        }

        private static MerchRequest CreateMerchRequestByModel(Models.MerchRequest merchRequestModel)
        {
            var merchRequest = new MerchRequest(
                merchRequestModel.Id,
                EmployeeId.Create(merchRequestModel.EmployeeId),
                Enumeration.GetById<RequestMerchType>(merchRequestModel.MerchType),
                Enumeration.GetById<ProcessStatus>(merchRequestModel.Status),
                Enumeration.GetById<CreationMode>(merchRequestModel.Mode),
                merchRequestModel.GiveOutDate.HasValue ? Date.Create(merchRequestModel.GiveOutDate.Value) : null,
                merchRequestModel.IsEmailSended
            );
            return merchRequest;
        }
    }
}