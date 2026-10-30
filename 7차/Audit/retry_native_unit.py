from pathlib import Path
import shlex
import subprocess

audit = Path(__file__).resolve().parent
log = (audit / 'AndroidBuild-initial-nativefailure.log').read_text(encoding='utf-8-sig')
line = next(x for x in log.splitlines() if 'Program arguments:' in x and 'Generics__28.cpp' in x)
args = shlex.split(line.split('Program arguments:', 1)[1].strip())
assert args[0].endswith('/AndroidPlayer/NDK/toolchains/llvm/prebuilt/windows-x86_64/bin/clang++')
assert args[-1].endswith('/Library/Bee/artifacts/Android/il2cppOutput/cpp/Generics__28.cpp')
args[0] += '.exe'
with (audit / 'NativeUnitRetry.log').open('w', encoding='utf-8') as output:
    result = subprocess.run(args, cwd=r'C:\Users\PUVKID\Documents\Codex\2026-10-13\new-chat\work\Build7', stdout=output, stderr=subprocess.STDOUT)
print('Single-unit native compiler exit:', result.returncode)
print((audit / 'NativeUnitRetry.log').read_text(encoding='utf-8')[-1600:])
raise SystemExit(result.returncode)
