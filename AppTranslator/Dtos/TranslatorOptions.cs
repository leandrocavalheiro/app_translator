namespace AppTranslator.Dtos;

public class TranslatorOptions{
    public string ResourcesPath { get; set; }
    public string ResourceName { get; set; }
    public string Languages { get; set; }
    public string DefaultLanguage { get; set; }
    public string DefaultContext { get; set; }
}