"""Build self-contained local reading pages from project Markdown (stdlib only).

Media remains relative inside the project. GitHub continues to use README.md.
"""
from pathlib import Path
import html
import re

ROOT=Path(__file__).resolve().parent.parent
TARGETS={
    'README.md':('docs/PROJECT-PREVIEW.html','../'),
    'docs/GITHUB-UPLOAD.zh-CN.md':('docs/QUICKSTART.zh-CN.html','./'),
    'docs/FEATURE-GALLERY.zh-CN.md':('docs/FEATURE-GALLERY.zh-CN.html','./'),
    'docs/CUSTOMIZE-CHARACTER.zh-CN.md':('docs/CUSTOMIZE-CHARACTER.zh-CN.html','./'),
    'docs/RELEASE-COPY.zh-CN.md':('docs/RELEASE-COPY.zh-CN.html','./'),
    'docs/QUOTA-LAYOUT.zh-CN.md':('docs/QUOTA-LAYOUT.zh-CN.html','./'),
}
CSS='''*{box-sizing:border-box}body{margin:0;color:#30313d;background:#f2f3f8;font:16px/1.9 "Segoe UI","Microsoft YaHei",sans-serif}nav{background:#242533;color:#fff;padding:16px 5%;position:sticky;top:0;z-index:2;display:flex;gap:24px;flex-wrap:wrap}nav a{color:#ded8ff;text-decoration:none}nav strong{margin-right:auto}main{max-width:1100px;margin:30px auto;padding:42px 52px;background:white;border-radius:24px;box-shadow:0 12px 40px #2923330b}h1{font-size:34px;line-height:1.4;color:#252436}h2{border-top:1px solid #e8e6ef;padding-top:27px;margin-top:36px;font-size:24px}h3{font-size:19px;margin-top:25px}a{color:#6552a0}img,video{display:block;max-width:100%;height:auto;border-radius:15px;margin:20px auto}pre{background:#272735;color:#f0edf8;padding:22px;border-radius:12px;white-space:pre-wrap;overflow-wrap:anywhere;line-height:1.7}code{font:14px Consolas,monospace;background:#f0edf7;padding:2px 5px;border-radius:4px}img[src$=".gif"]{image-rendering:pixelated}pre code{background:none;padding:0}blockquote{margin:20px 0;border-left:4px solid #a893d0;background:#f7f5fb;padding:12px 22px;color:#686278}table{border-collapse:collapse;width:100%;margin:20px 0;font-size:14px}td,th{padding:12px;text-align:left;border-bottom:1px solid #e7e6ed}th{background:#f6f4fa}li{margin:8px 0}details{padding:16px 22px;background:#f8f7fb;border-radius:12px}summary{cursor:pointer;font-weight:bold}footer{color:#898492;font-size:13px;margin-top:40px}.tag{color:#827095;font-size:13px;letter-spacing:1px}@media(max-width:700px){main{margin:0;padding:24px 18px;border-radius:0}h1{font-size:28px}nav{position:static;gap:12px}table{display:block;overflow:auto}}'''

def inline(text):
    text=html.escape(text)
    text=re.sub(r'`([^`]+)`',r'<code>\1</code>',text)
    text=re.sub(r'\*\*([^*]+)\*\*',r'<strong>\1</strong>',text)
    def target(m):
        label,url=m.groups()
        for source,(destination,_) in TARGETS.items():
            old=source if url.startswith('docs/') else Path(source).name
            new=destination if url.startswith('docs/') else Path(destination).name
            if url==old or url.startswith(old+'#'):url=new+url[len(old):];break
        return '<a href="'+url+'">'+label+'</a>'
    text=re.sub(r'!\[([^\]]*)\]\(([^)]+)\)',r'<img alt="\1" src="\2" loading="lazy">',text)
    return re.sub(r'\[([^\]]+)\]\(([^)]+)\)',target,text)

def render(markdown):
    result=[];paragraph=[];table=[];listing=None;code=None
    def flush():
        nonlocal listing
        if paragraph:result.append('<p>'+inline(' '.join(paragraph))+'</p>');paragraph.clear()
        if table:
            rows=[]
            for i,line in enumerate(table):
                if re.fullmatch(r'[\s|:\-]+',line):continue
                cells=line.strip('|').split('|');tag='th' if i==0 else 'td'
                rows.append('<tr>'+''.join('<'+tag+'>'+inline(v.strip())+'</'+tag+'>' for v in cells)+'</tr>')
            result.append('<table>'+''.join(rows)+'</table>');table.clear()
        if listing:result.append('</'+listing+'>');listing=None
    for line in markdown.splitlines()+['']:
        if line.startswith('```'):
            if code is None:flush();code=[]
            else:result.append('<pre><code>'+html.escape('\n'.join(code))+'</code></pre>');code=None
            continue
        if code is not None:code.append(line);continue
        if not line.strip():flush();continue
        if line.startswith('|'):table.append(line);continue
        if line.startswith(('<div','</div','<details','</details','<summary','</summary')):flush();result.append(line);continue
        heading=re.match(r'^(#{1,6}) (.+)',line)
        if heading:
            flush();level=len(heading[1]);slug=re.sub(r'[^\w\s-]','',heading[2]).strip().lower().replace(' ','-')
            result.append(f'<h{level} id="{slug}">'+inline(heading[2])+f'</h{level}>');continue
        if line.startswith('> '):flush();result.append('<blockquote>'+inline(line[2:])+'</blockquote>');continue
        item=re.match(r'^(?:([-*]) |\d+\. )(.+)',line)
        if item:
            kind='ul' if item[1] else 'ol'
            if listing!=kind:flush();result.append('<'+kind+'>');listing=kind
            result.append('<li>'+inline(item[2])+'</li>');continue
        if listing:flush()
        paragraph.append(line)
    return '\n'.join(result)

def main():
    for source,(destination,base) in TARGETS.items():
        text=(ROOT/source).read_text(encoding='utf-8-sig')
        prefix='docs/' if base=='../' else ''
        nav=''.join('<a href="'+prefix+file+'">'+label+'</a>' for file,label in [('PROJECT-PREVIEW.html','项目首页'),('FEATURE-GALLERY.zh-CN.html','功能图鉴'),('CUSTOMIZE-CHARACTER.zh-CN.html','改角色'),('RELEASE-COPY.zh-CN.html','发布文案')])
        page='<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><base href="'+base+'"><title>PetBridge · 中文阅读版</title><style>'+CSS+'</style><nav><strong>PetBridge</strong>'+nav+'</nav><main><div class="tag">WINDOWS DESKTOP COMPANION · v1.3.1</div>'+render(text)+'<footer>本地阅读版；GitHub 首页由 README.md 展示。图片与 GIF 随项目保存，无需登录即可阅读。</footer></main></html>'
        (ROOT/destination).write_text(page,encoding='utf-8')
    print('Built '+str(len(TARGETS))+' local reading pages.')

if __name__=='__main__':main()
