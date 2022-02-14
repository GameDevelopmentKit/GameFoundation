namespace GameFoundation.Scripts.Network.ApiHandler
{
    using GameFoundation.Scripts.Network.Authentication;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.LogService;
    using MechSharingCode.WebService.Authentication;

    public class SendOTPRequest : BaseHttpRequest<OtpSendResponseDataData>
    {
        private readonly DataLoginServices dataLoginServices;
        public SendOTPRequest(ILogService logger, DataLoginServices dataLoginServices) : base(logger) { this.dataLoginServices = dataLoginServices; }
        public override void Process(OtpSendResponseDataData responseDataData) { this.dataLoginServices.SendCodeStatus.Value   = SendCodeStatus.Sent; }

        public override void ErrorProcess(int statusCode)
        {
            switch (statusCode)
            {
                case 0:
                    this.dataLoginServices.SendCodeStatus.Value = SendCodeStatus.EmailIsInvalid;
                    break;
                case 1:
                    this.dataLoginServices.SendCodeStatus.Value = SendCodeStatus.TooManyRequest;
                    break;
            }
        }
    }
}