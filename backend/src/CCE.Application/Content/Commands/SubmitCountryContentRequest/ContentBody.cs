using System.Text.Json.Serialization;

namespace CCE.Application.Content.Commands.SubmitCountryContentRequest;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateNewsBody), typeDiscriminator: "news")]
[JsonDerivedType(typeof(CreateEventBody), typeDiscriminator: "event")]
[JsonDerivedType(typeof(CreateResourceBody), typeDiscriminator: "resource")]
public abstract record ContentBody;
