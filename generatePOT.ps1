xgettext --from-code=UTF-8 --language=C# `
  --keyword=Tr:1,2 --keyword=TranslationServer.Tr:1,2 `
  --keyword=I18N.Tr:2,1 `
  -o "./src/clientv4/ResourcePack/core/language/template.pot" `
  @((Get-ChildItem -Path . -Recurse -Filter *.cs | ForEach-Object { Resolve-Path $($_.FullName) -Relative }))