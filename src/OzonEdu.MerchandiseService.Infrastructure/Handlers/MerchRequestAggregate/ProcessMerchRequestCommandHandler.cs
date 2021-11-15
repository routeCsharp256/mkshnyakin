﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OzonEdu.MerchandiseService.Domain.AggregationModels.MerchRequestAggregate;
using OzonEdu.MerchandiseService.Infrastructure.ApplicationServices;
using OzonEdu.MerchandiseService.Infrastructure.Commands.MerchRequestAggregate;
using OzonEdu.MerchandiseService.Infrastructure.Exceptions;
using OzonEdu.MerchandiseService.Infrastructure.Extensions;
using static OzonEdu.MerchandiseService.Domain.DomainServices.MerchRequestService;

namespace OzonEdu.MerchandiseService.Infrastructure.Handlers.MerchRequestAggregate
{
    public class ProcessMerchRequestCommandHandler
        : IRequestHandler<ProcessMerchRequestCommand, MerchRequestResult>
    {
        private readonly IApplicationService _applicationService;
        private readonly IMerchRequestRepository _merchRequestRepository;

        public ProcessMerchRequestCommandHandler(
            IMerchRequestRepository merchRequestRepository,
            IApplicationService applicationService)
        {
            _merchRequestRepository = merchRequestRepository;
            _applicationService = applicationService;
        }

        public async Task<MerchRequestResult> Handle(
            ProcessMerchRequestCommand command,
            CancellationToken cancellationToken)
        {
            var requestMerchType = command.MerchType.ToRequestMerchType();
            var creationMode = command.IsSystem ? CreationMode.System : CreationMode.User;
            var employeeEmail = command.EmployeeEmail;
            var employeeId = command.EmployeeId;

            var employee = await _applicationService.GetEmployee(employeeId, employeeEmail, cancellationToken)
                           ?? throw new ItemNotFoundException(
                               $"Employee (id:{employeeId}, email: {employeeEmail}) is not found");

            var employeeMerchRequests =
                await _applicationService.GetEmployeeMerchRequests(employee.Id, cancellationToken);

            var request = ProcessUserMerchRequest(
                employee,
                requestMerchType,
                creationMode,
                employeeMerchRequests,
                Date.Create(DateTime.Now));

            // Проверяется что такой мерч еще не выдавался сотруднику
            if (request.Status.Equals(ProcessStatus.Complete))
            {
                return MerchRequestResult.Fail(request.Status.ToString(), request.Id);
            }


            // Проверяется наличие данного мерча на складе через запрос к stock-api
            // Если все проверки прошли - зарезервировать мерч в stock-api
            var isComplete = await _applicationService.CheckAndReserve(request, employee, cancellationToken);
            if (isComplete)
            {
                // Отметить у себя в БД, что сотруднику выдан мерч
                request.Complete(Date.Create(DateTime.Now));
            }
            else
            {
                //Если мерча нет в наличии - необходимо запомнить, что такой сотрудник запрашивал такой мерч
                request.SetStatus(ProcessStatus.OutOfStock);
            }

            var savedRequest = await SaveRequest(request, cancellationToken);

            var response = savedRequest.Status.Equals(ProcessStatus.Complete)
                ? MerchRequestResult.Success(savedRequest.Status.ToString(), savedRequest.Id)
                : MerchRequestResult.Fail(savedRequest.Status.ToString(), savedRequest.Id);

            return await Task.FromResult(response);
        }

        private async Task<MerchRequest> SaveRequest(MerchRequest merchRequest, CancellationToken cancellationToken)
        {
            if (merchRequest.Id == 0)
            {
                merchRequest = await _merchRequestRepository.CreateAsync(merchRequest, cancellationToken);
            }
            else
            {
                merchRequest = await _merchRequestRepository.UpdateAsync(merchRequest, cancellationToken);
            }
            return merchRequest;
        }
    }
}