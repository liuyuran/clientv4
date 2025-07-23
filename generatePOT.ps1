xgettext --from-code=UTF-8 --language=C# `
  --keyword=Tr:1,2c --keyword=TranslationServer.Translate:1,2c `
  --keyword=I18N.Tr:2,1c `
  -o "./src/clientv4/ResourcePack/core/language/template.pot" `
  @((Get-ChildItem -Path . -Recurse -Filter *.cs | ForEach-Object { Resolve-Path $($_.FullName) -Relative }))