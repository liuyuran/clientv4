@echo off
rd /s /q "generated"
for /R %%G in (*.fbs) do (
    flatc -I .\proto --csharp "%%G"
)