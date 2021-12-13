﻿using System;
using System.Collections.Generic;
using System.Text;
using Sigma3.Services.Web;
using SQLite;
using Sigma3.Util;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace Sigma3.Objects
{
    public class User
    {
        [PrimaryKey]
        public string Id { get; set; }

        public  bool porfolioHidden = false;
        public string Name { get; set; }
        public string Email { get; set; }   
        public string Password { get; set; } 
        public string PhoneNumber { get; set; }
        public decimal PortfolioBalance { get; set; } = 0;

        public List<SecuritiesModel> UserPortfolioList = new List<SecuritiesModel>();
        public List<SecuritiesModel> UserFollowing { get; set; } = new List<SecuritiesModel>();
        public List<TransactionModel> Transactions { get; set; } = new List<TransactionModel>();
        public Dictionary<string, UserSecurity> UserPortfolio { get; set; } = new Dictionary<string, UserSecurity>();


        async public Task<bool> AddTransaction(TransactionModel transaction, SecuritiesModel model)
        {
            var Symbol = transaction.SecurityTraded;
            var isBuy = transaction.TransType.Equals("BUY");
            // slow 
            var ParseRegularMarketPrice = decimal.Parse(model.RegularMarketPrice.ToString());


            transaction.UserId = Id;
            var response =  await SigmaTransaction.SendPostAsync(transaction);

            Transactions.Add(transaction);
            
            // I dont believe this is thread-safe.. ConcurrentDict would probably be better?
            if (UserPortfolio.ContainsKey(Symbol))
            {
                var element = UserPortfolio[Symbol];
                if (isBuy)
                { 
                    element.BuyTimes += 1;
                    element.AmountOwned += transaction.AmountTransacted;
                    PortfolioBalance += (ParseRegularMarketPrice * transaction.AmountTransacted);
                    
                }
                else
                {
                    element.SellTimes += 1;
                    element.AmountOwned -= transaction.AmountTransacted;
                    PortfolioBalance -= (ParseRegularMarketPrice * transaction.AmountTransacted);

                    if (element.AmountOwned == 0)
                    {
                        UserPortfolio.Remove(Symbol);
                    }
                }

            }
            else
            {
                if (isBuy)
                {
                    UserPortfolio[Symbol] = new UserSecurity(Symbol, transaction.AmountTransacted, transaction.PricePerSecurity, 1, model.ShortName);
                    PortfolioBalance += (ParseRegularMarketPrice * transaction.AmountTransacted);

                }
                else
                {
                    UserPortfolio[Symbol] = new UserSecurity(transaction.AmountTransacted, Symbol, transaction.PricePerSecurity, 1, model.ShortName);
                    PortfolioBalance -= (ParseRegularMarketPrice * transaction.AmountTransacted);

                }
            }



            return response != null;
         
        }

        // figure out a way to lazy load this
        async public Task<List<UserPortfolioObject>> GetUserPortfolio()
        {

          
            var keys = UserPortfolio.Keys;
            var values = new List<UserPortfolioObject>();
            foreach (var key in keys)
            {
                var item = await SecuritiesApi.GetAsync(key);
                values.Add(new UserPortfolioObject(item, UserPortfolio[key]));
            }
            return values;
        }

  


        public class UserSecurity
        {
            public string Symbol { get; set; }
            public decimal AmountOwned { get; set; }
            public decimal AveragePrice { get; set; }
            public int BuyTimes { get; set; } = 0;
            public int SellTimes { get; set; } = 0;
            public string ShortName { get; set; }

            public UserSecurity(string symbol, decimal AmountOwned, decimal AveragePrice, int BuyTimes, int SellTimes)
            {
                this.Symbol = symbol;
                this.AmountOwned = AmountOwned;
                this.AveragePrice = AveragePrice;
                this.BuyTimes = BuyTimes;
                this.SellTimes = SellTimes;
            }

            public UserSecurity(string symbol, decimal AmountOwned, decimal AveragePrice, int BuyTimes, string name)
            {
                this.Symbol = symbol;
                this.AmountOwned = AmountOwned;
                this.AveragePrice = AveragePrice;
                this.BuyTimes = BuyTimes;
                this.ShortName = name;
            }
            public UserSecurity(decimal AmountOwned, string symbol, decimal AveragePrice, int SellTimes, string name)
            {
                this.Symbol = symbol;
                this.AmountOwned = AmountOwned;
                this.AveragePrice = AveragePrice;
                this.SellTimes = SellTimes;
                this.ShortName = name;
            }
        }

        public class UserPortfolioObject
        {
            public string SecurityName { get; set; }
            public string SecuritySymbol { get; set; }
            public string UnderName { get; set; }
            public string TotalOwned { get; set; }
            public string SecurityPrice { get; set; }
            public UserPortfolioObject(SecuritiesModel model, UserSecurity us)
            {
                var dec = decimal.Parse(model.RegularMarketPrice.ToString());
                this.SecurityPrice = model.RegularMarketPriceProp;
                // lol its 7am
                this.UnderName = $"{StringUtils.ParseNumberWithCommas(decimal.Parse(us.AmountOwned.ToString()))} | {model.RegularMarketPriceProp}";
                this.TotalOwned = $"${StringUtils.ParseNumberWithCommas (dec * us.AmountOwned)}";
                this.SecurityName = us.ShortName;
                this.SecuritySymbol = us.Symbol;
            }

            public UserPortfolioObject()
            {

            }
        }



        async public void AddFollowing(string symbole)
        {
           
            UserFollowing.Add(await SecuritiesApi.GetAsync(symbole));

        }
        public void RemoveFollowing(string symbol)
        {
            UserFollowing.RemoveAll(security => security.Symbol.Equals(symbol));

        }

    }
}
