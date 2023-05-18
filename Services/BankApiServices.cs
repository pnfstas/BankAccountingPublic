using BankAccountingApi.Helpers;
using BankAccountingApi.Models;
using Microsoft.AspNetCore.Identity;

namespace BankAccountingApi.Services
{
    public interface ITokenStoreService
    {
        string StoreToken(string key, string token);
        bool VerifyToken(string key, string token);
        bool RemoveToken(string key);
        string GetToken(string key);
    }
    public class TokenStoreService : ITokenStoreService
    {
        private Dictionary<string, string> TokenStore { get; set; }
        public TokenStoreService()
        {
            TokenStore = new Dictionary<string, string>();
        }
        public string StoreToken(string key, string token)
        {
            string result = null;
            try
            {
                if(TokenStore != null)
                {
                    TokenStore[key] = token;
                    if(!TokenStore.TryGetValue(key, out result))
                    {
                        result = null;
                    }
                }
            }
            catch(Exception)
            {
                result = null;
            }
            return result;
        }
        public bool VerifyToken(string key, string token)
        {
            bool result = false;
            try
            {
                string value = null;
                if(TokenStore?.TryGetValue(key, out value) == true)
                {
                    result = string.Equals(token, value);
                    TokenStore.Remove(key);
                }
            }
            catch(Exception)
            {
                result = false;
            }
            return result;
        }
        public bool RemoveToken(string key)
        {
            bool result = false;
            try
            {
                result = TokenStore?.ContainsKey(key) == true && TokenStore.Remove(key);
            }
            catch(Exception)
            {
                result = false;
            }
            return result;
        }
        public string GetToken(string key)
        {
            string token = null;
            if(TokenStore?.TryGetValue(key, out token) != true)
            {
                token = null;
            }
            return token;
        }
    }
    class BankApiUserTwoFactorTokenProvider : IUserTwoFactorTokenProvider<BankApiUser>
    {
        private ITokenStoreService TokenStore { get; set; }
        public BankApiUserTwoFactorTokenProvider(ITokenStoreService tokenStore)
        {
            TokenStore = tokenStore;
        }
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<BankApiUser> manager, BankApiUser user)
        {
            return Task.FromResult(manager.SupportsUserTwoFactor);
        }
        public Task<string> GenerateAsync(string purpose, UserManager<BankApiUser> manager, BankApiUser user)
        {
            BankApiUserOptions userOptions = Startup.Startup.UserOptions;
            string token = SecurityHelper.GenerateRandomIntLambda(userOptions.VerificationCodeLength);
            token = TokenStore.StoreToken(user.Id, token);
            return Task.FromResult(token);
        }
        public Task<bool> ValidateAsync(string purpose, string token, UserManager<BankApiUser> manager, BankApiUser user)
        {
            bool result = TokenStore.VerifyToken(user.Id, token);
            return Task.FromResult(result);
        }
    }
}
