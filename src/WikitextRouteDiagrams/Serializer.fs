namespace WikitextRouteDiagrams

open System
open System.Text.Json
open System.Text.Json.Serialization

open Microsoft.FSharp.Reflection

module Serializer =

    type UnionValueConverter<'a>() =
        inherit JsonConverter<'a>()

        static member FromString<'a>(s: string) =
            match FSharpType.GetUnionCases typeof<'a>
                  |> Array.filter (fun case -> String.Compare(case.Name, s, StringComparison.OrdinalIgnoreCase) = 0) with
            | [| case |] -> Some(FSharpValue.MakeUnion(case, [||]) :?> 'a)
            | _ -> None

        override this.Read(reader: byref<Utf8JsonReader>, _typ: Type, options: JsonSerializerOptions) =
            let s =
                JsonSerializer.Deserialize<string>(&reader, options)

            match UnionValueConverter.FromString<'a> s with
            | Some v -> v
            | None -> failwith (sprintf "connot convert value '%s'" s)

        override this.Write(writer: Utf8JsonWriter, value: 'a, options: JsonSerializerOptions) =
            JsonSerializer.Serialize(writer, value.ToString(), options)

    type UnionConverter<'a>() =
        inherit JsonConverterFactory()
        override this.CanConvert(t: Type): bool = t.Name = typedefof<'a>.Name

        override this.CreateConverter(typeToConvert: Type, _options: JsonSerializerOptions): JsonConverter =
            let converterType =
                typedefof<UnionValueConverter<_>>.MakeGenericType(typeToConvert)

            Activator.CreateInstance(converterType) :?> JsonConverter

    let deserializeOptions = JsonSerializerOptions()

    let serializeOptions =
        let jso = JsonSerializerOptions(IgnoreNullValues = true)
        jso.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        jso

    let addConverters (deserializeConverters: JsonConverter array) =
        if (deserializeOptions.Converters.Count = 0) then
            for converter in deserializeConverters do
                deserializeOptions.Converters.Add(converter)
            deserializeOptions.Converters.Add(JsonFSharpConverter())
            serializeOptions.Converters.Add(JsonFSharpConverter())

    let Deserialize<'a> (response: string) =
        JsonSerializer.Deserialize<'a>(response, deserializeOptions)

    let Serialize<'a> (o: 'a) =
        JsonSerializer.Serialize<'a>(o, serializeOptions)
