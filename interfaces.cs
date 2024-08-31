
public interface IDetails
{
  string Service { get; set; }
  string OnUSCheck { get; set; }
  string CashIn { get; set; }
  string CashOut { get; set; }
  string NotOnUSCheck { get; set; }
  string Taxes { get; set; }

}

public class Details : IDetails
{
  public string Service { get; set; }
  public string OnUSCheck { get; set; }
  public string CashIn { get; set; }
  public string CashOut { get; set; }
  public string NotOnUSCheck { get; set; }
  public string Taxes { get; set; }
}

public interface IInfoJson
{
  string InvoiceNumber { get; set; }
  string Date { get; set; }
  string BilledTo { get; set; }
  string BusinessNumberInBR { get; set; }
  Details[] Details { get; set; }
}

public class InfoJson : IInfoJson
{
  public string InvoiceNumber { get; set; }
  public string Date { get; set; }
  public string BilledTo { get; set; }
  public string BusinessNumberInBR { get; set; }
  public Details[] Details { get; set; }
}