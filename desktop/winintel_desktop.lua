-- GPL-3.0-or-later. Optional asynchronous local AI ranking.
local M = {}
local function clean(s) return (s or ""):gsub("[\r\n\t]", " "):sub(1, 512) end
function M.init(env)
  env.dir = rime_api.get_user_data_dir() .. "/winintel_bridge"
  env.previous, env.context = "", ""
  env.commit = env.engine.context.commit_notifier:connect(function(ctx)
    env.context = (env.context .. (ctx:get_commit_text() or "")):sub(-160):gsub("^[\128-\191]+", "")
    env.previous = ""
  end)
end
function M.fini(env) if env.commit then env.commit:disconnect() end end
local function flush(head, texts, code, env)
  local emitted = {}
  local response = io.open(env.dir .. "/response.tsv", "r")
  if response then
    local input, context = response:read("*l"), response:read("*l")
    if input == clean(code) and context == clean(env.context) then
      for text in response:lines() do
        for i, candidate in ipairs(head) do
          if candidate.text == text and not emitted[i] then yield(candidate); emitted[i] = true; break end
        end
      end
    end
    response:close()
  end
  for i, candidate in ipairs(head) do if not emitted[i] then yield(candidate) end end
  if code ~= "" and #code <= 64 and code ~= env.previous then
    local request = io.open(env.dir .. "/request.tmp", "w")
    if request then
      request:write(clean(code), "\n", clean(env.context), "\n")
      for _, text in ipairs(texts) do request:write(clean(text), "\n") end
      request:close()
      os.remove(env.dir .. "/request.tsv")
      os.rename(env.dir .. "/request.tmp", env.dir .. "/request.tsv")
      env.previous = code
    end
  end
end
function M.func(translation, env)
  local code = env.engine.context.input or ""
  if code == "" then
    for candidate in translation:iter() do yield(candidate) end
    return
  end
  local head, texts = {}, {}
  -- Keep Lua's iterator state intact; never split the returned iterator tuple.
  for candidate in translation:iter() do
    if #head < 9 then
      head[#head + 1], texts[#texts + 1] = candidate, candidate.text
      if #head == 9 then flush(head, texts, code, env) end
    else yield(candidate) end
  end
  if #head < 9 then flush(head, texts, code, env) end
end
return M
