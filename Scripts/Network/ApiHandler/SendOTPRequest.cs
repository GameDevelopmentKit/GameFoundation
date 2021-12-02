namespace GameFoundation.Scripts.Network.ApiHandler
{
    using GameFoundation.Scripts.Network.Authentication;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.LogService;
    using MechSharingCode.WebService.Authentication;
    using UniRx;

    public class SendOTPRequest : BaseHttpRequest<OtpSendResponseData>
    {
        private readonly DataLoginServices dataLoginServices;
        public SendOTPRequest(ILogService logger, DataLoginServices dataLoginServices) : base(logger) { this.dataLoginServices = dataLoginServices; }
        public override void Process(OtpSendResponseData responseData)
        {
            if (responseData.Status == 1)
            {
                this.dataLoginServices.SendCodeComplete.Value = SendCodeStatus.Complete;
            }
            else
            {
                this.dataLoginServices.SendCodeComplete.Value = SendCodeStatus.Error;
            }
        }
    }
}