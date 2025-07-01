# 体素游戏实验型第四版

### 初衷
仅仅是想复刻一个能有更好的mod支持的minecraft原型而已，其他都只是意外之喜。

### 生成POT翻译文件（仅限于Linux/WSL2可执行）
```xgettext --from-code=UTF-8 --language=C# --keyword=Tr:1,2 --keyword=TranslationServer.Translate:1,2 --keyword=I18N.Tr:2,1 -o ./src/clientv4/ResourcePack/core/language/template.pot $(find . -name '*.cs')```
