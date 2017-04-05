namespace AdviceSlipApiWrapper
{
    public class AdviceSlip
    {
        public string Advice { get; set; }
        public int Id { get; set; }

        public AdviceSlip (string advice, int id)
        {
            Advice = advice;
            Id = id;
        }

        public AdviceSlip (string advice, string id) : this (advice, int.Parse(id)) { }
    }
}
