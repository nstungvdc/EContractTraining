using System;
using System.Collections.Generic;
using System.Text;

namespace EContractQuiz1.Models
{
    public class CredentialResponseModel
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
        public object roles { get; set; }
        public string partyId { get; set; }
        public string jti { get; set; }
    }
}
