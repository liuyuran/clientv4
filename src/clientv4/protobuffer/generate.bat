@echo off
rd /s /q "generated"
flatc -I .\proto --csharp .\proto\block-define.fbs
flatc -I .\proto --csharp .\proto\server-meta.fbs