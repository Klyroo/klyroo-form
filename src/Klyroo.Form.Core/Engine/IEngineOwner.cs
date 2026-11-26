namespace Klyroo.Form;

// A public controller that owns an engine. The components of Klyroo.Form take an IFormRuntime and reach the engine through this, so one component serves
// the typed controller and the dynamic one.
internal interface IEngineOwner
{
    FormEngine Engine { get; }
}
