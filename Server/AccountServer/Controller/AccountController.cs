using AccountServer.DB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AccountServer.DB.DataModel;

namespace AccountServer.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        AppDbContext _context;
        SharedDbContext _shared;
        
        public AccountController(AppDbContext contetxt, SharedDbContext shared)
        {
            _context = contetxt;
            _shared = shared;
        }

        // 최종 경로 -> api/account/create
        [HttpPost]
        [Route("create")]
        public CreateAccountPacketRes CreateAccount([FromBody] CreateAccountPacketReq req)
        {
            CreateAccountPacketRes res = new CreateAccountPacketRes();

            AccountDb account =_context.Accounts
                                    .AsNoTracking()
                                    .Where(a => a.AccountName == req.AccountName)
                                    .FirstOrDefault();


            if (account == null)
            {
                _context.Accounts.Add(new AccountDb()
                {
                    AccountName = req.AccountName,
                    Password = req.Password
                });

                bool success = _context.SaveChangesEx();
                res.CreateOk = success;
            }
            else
            {
                res.CreateOk = false;
            }


            return res;
        }

		[HttpPost]
		[Route("login")]
		public LoginAccountPacketRes LoginAccount([FromBody] LoginAccountPacketReq req)
		{
			LoginAccountPacketRes res = new LoginAccountPacketRes();

			AccountDb account = _context.Accounts
				.AsNoTracking()
				.Where(a => a.AccountName == req.AccountName && a.Password == req.Password)
				.FirstOrDefault();

			if (account == null)
			{
				res.LoginOk = false;
			}
			else
			{
                res.AccountId = account.AccountDbId;
				res.LoginOk = true;
			}

			return res;
		}
	}
}
