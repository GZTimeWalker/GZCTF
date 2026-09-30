import type { ChoiceQuestion, ChoiceType } from './ChoiceApi'

export const optionLabel = (index: number) => String.fromCharCode(65 + index)

export function parseAnswerLetters(value: string): number[] {
  if (!/^[A-Z\s,，;；|]+$/i.test(value.trim())) throw new Error('答案须使用 A–Z 选项字母，例如 A 或 A,C。')
  return [...value.toUpperCase().replace(/[\s,，;；|]/g, '')].map((c) => c.charCodeAt(0) - 65)
}

export function validateQuestion(input: unknown, row: number): ChoiceQuestion {
  const q = input as ChoiceQuestion
  const fail = () => {
    throw new Error(`第 ${row} 道题目的题型、题干、选项或答案无效。`)
  }
  if (
    !q ||
    !['Single', 'Multiple'].includes(q.type) ||
    typeof q.content !== 'string' ||
    !q.content.trim() ||
    q.content.length > 10000 ||
    !Array.isArray(q.options) ||
    q.options.length < 2 ||
    q.options.length > 26 ||
    q.options.some((o) => typeof o !== 'string' || !o.trim() || o.length > 2000) ||
    new Set(q.options).size !== q.options.length ||
    !Array.isArray(q.correctAnswers) ||
    q.correctAnswers.some((a) => !Number.isInteger(a) || a < 0 || a >= q.options.length) ||
    new Set(q.correctAnswers).size !== q.correctAnswers.length ||
    (q.type === 'Single' ? q.correctAnswers.length !== 1 : q.correctAnswers.length < 2)
  )
    fail()
  return {
    type: q.type,
    content: q.content.trim(),
    options: q.options.map((o) => o.trim()),
    correctAnswers: q.correctAnswers,
  }
}

// RFC 4180: support BOM, CRLF, quoted commas, escaped quotes and multiline fields.
export function parseCsv(text: string): string[][] {
  const rows: string[][] = []
  let row: string[] = [],
    field = '',
    quoted = false,
    closed = false
  for (let i = 0; i < text.length; i++) {
    const c = text[i]
    if (quoted) {
      if (c === '"' && text[i + 1] === '"') {
        field += '"'
        i++
      } else if (c === '"') {
        quoted = false
        closed = true
      } else field += c
    } else if (c === ',' || c === '\n' || c === '\r') {
      row.push(field)
      field = ''
      closed = false
      if (c !== ',') {
        if (row.some((v) => v.trim())) rows.push(row)
        row = []
        if (c === '\r' && text[i + 1] === '\n') i++
      }
    } else if (c === '"' && !field && !closed) quoted = true
    else {
      if (closed || c === '"') throw new Error('CSV 引号格式无效。')
      field += c
    }
  }
  if (quoted) throw new Error('CSV 存在未闭合的引号。')
  row.push(field)
  if (row.some((v) => v.trim())) rows.push(row)
  return rows
}

export function parseChoiceImport(text: string, filename: string): ChoiceQuestion[] {
  text = text.replace(/^\uFEFF/, '')
  let questions: unknown[]
  if (filename.toLowerCase().endsWith('.csv')) {
    const [header, ...rows] = parseCsv(text)
    if (!header || header.join(',') !== 'type,content,options,answers')
      throw new Error('CSV 表头须为 type,content,options,answers。')
    questions = rows.map((row, index) => {
      if (row.length !== 4) throw new Error(`CSV 第 ${index + 2} 行须有四列。`)
      const [type, content, options, answers] = row
      return {
        type: ({ 单选: 'Single', 多选: 'Multiple', single: 'Single', multiple: 'Multiple' }[
          type.trim().toLowerCase()
        ] ?? type.trim()) as ChoiceType,
        content,
        options: options.split('|').map((o) => o.trim()),
        correctAnswers: parseAnswerLetters(answers),
      }
    })
  } else {
    const parsed = JSON.parse(text)
    questions = Array.isArray(parsed) ? parsed : parsed.questions
  }
  if (!Array.isArray(questions) || !questions.length || questions.length > 5000)
    throw new Error('请导入包含 1–5000 道题目的 JSON 数组或 CSV 文件。')
  return questions.map((q, i) => validateQuestion(q, i + 1))
}

export const exampleQuestions: ChoiceQuestion[] = [
  { type: 'Single', content: 'HTTP 默认端口是？', options: ['80', '443', '22', '53'], correctAnswers: [0] },
  {
    type: 'Multiple',
    content: '下列哪些属于非对称加密算法？',
    options: ['RSA', 'AES', 'ECC', 'DES'],
    correctAnswers: [0, 2],
  },
]

export function downloadChoiceFile(filename: string, content: string, type = 'application/json') {
  const url = URL.createObjectURL(new Blob([content], { type }))
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  anchor.click()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}
