﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpCourse.Core.Lib.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenTracing;
using OzonEdu.MerchandiseService.HttpModels;
using OzonEdu.MerchandiseService.Infrastructure.Commands.MerchRequestAggregate;
using OzonEdu.MerchandiseService.Infrastructure.Exceptions;

namespace OzonEdu.MerchandiseService.Controllers.V1
{
    [ApiController]
    [Route("api/v1/employee/{employeeId:int}/merch")]
    [Produces("application/json")]
    public class EmployeeMerchController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ITracer _tracer;

        public EmployeeMerchController(IMediator mediator, ITracer tracer)
        {
            _mediator = mediator;
            _tracer = tracer;
        }

        /// <summary>
        ///     Запрос ранее выданного мерча сотруднику
        /// </summary>
        /// <param name="employeeId">Идентификатор сотрудника</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(EmployeeMerchGetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeMerchGetResponse>> GetHistoryForEmployee(
            long employeeId,
            CancellationToken token)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(EmployeeMerchController)}.{nameof(GetHistoryForEmployee)}")
                .StartActive();
            span.Span.SetTag("protocol", "http");
            span.Span.SetTag(nameof(employeeId), employeeId);
            
            var getMerchRequestHistoryForEmployeeIdCommand = new GetMerchRequestHistoryForEmployeeIdCommand
            {
                EmployeeId = employeeId
            };

            try
            {
                var results = await _mediator.Send(getMerchRequestHistoryForEmployeeIdCommand, token);

                var historyItems = results.ToList();
                if (historyItems.Count == 0)
                {
                    var notFoundResponse = new RestErrorResponse
                    {
                        Status = StatusCodes.Status404NotFound,
                        Message = $"Merch history not found for employee {employeeId}"
                    };
                    return NotFound(notFoundResponse);
                }

                var response = new EmployeeMerchGetResponse
                {
                    Items = historyItems
                        .Select(x => new EmployeeMerchGetResponseItem
                        {
                            Item = new EmployeeMerchItem
                            {
                                Name = x.Item.Name,
                                SkuId = x.Item.Sku
                            },
                            Date = x.GiveOutDate
                        })
                };

                return Ok(response);
            }
            catch (ItemNotFoundException e)
            {
                var notFoundResponse = new RestErrorResponse
                {
                    Status = StatusCodes.Status404NotFound,
                    Message = e.Message
                };
                return NotFound(notFoundResponse);
            }
        }

        /// <summary>
        ///     Запрос на выдачу мерча для сотрудника
        /// </summary>
        /// <param name="employeeId">Идентификатор сотрудника</param>
        /// <param name="merchType">Тип мерча. Если пустой, то WelcomePack (10)</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("{merchType}")]
        [ProducesResponseType(typeof(EmployeeMerchPostResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RestErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(RestErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeMerchPostResponse>> RequestMerchForEmployee(
            long employeeId,
            MerchType merchType,
            CancellationToken token)
        {
            using var span = _tracer
                .BuildSpan($"{nameof(EmployeeMerchController)}.{nameof(RequestMerchForEmployee)}")
                .StartActive();
            span.Span.SetTag("protocol", "http");
            span.Span.SetTag(nameof(employeeId), employeeId);
            span.Span.SetTag(nameof(merchType), merchType.ToString());

            var processUserMerchRequestCommand = new ProcessMerchRequestCommand
            {
                EmployeeId = employeeId,
                MerchType = merchType,
                IsSystem = false
            };
            try
            {
                var result = await _mediator.Send(processUserMerchRequestCommand, token);
                span.Span.SetTag(nameof(result.IsSuccess), result.IsSuccess);
                if (!result.IsSuccess)
                {
                    var conflictResponse = new RestErrorResponse
                    {
                        Status = StatusCodes.Status409Conflict,
                        Message = result.Message
                    };
                    return Conflict(conflictResponse);
                }

                var response = new EmployeeMerchPostResponse
                {
                    RequestId = result.RequestId,
                    Message = result.Message
                };

                var uri = Url.Action(nameof(GetHistoryForEmployee), new {employeeId});
                return Created(uri, response);
            }
            catch (ItemNotFoundException e)
            {
                var notFoundResponse = new RestErrorResponse
                {
                    Status = StatusCodes.Status404NotFound,
                    Message = e.Message
                };
                return NotFound(notFoundResponse);
            }
        }
    }
}