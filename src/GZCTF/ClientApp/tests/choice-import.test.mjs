import assert from 'node:assert/strict'
import { test } from 'node:test'
import { exampleQuestions, parseChoiceImport, parseCsv } from '../src/utils/ChoiceImport.ts'

test('JSON template round-trips and supports a questions wrapper', () => {
  assert.deepEqual(parseChoiceImport(JSON.stringify(exampleQuestions), 'bank.json'), exampleQuestions)
  assert.deepEqual(parseChoiceImport(JSON.stringify({ questions: exampleQuestions }), 'bank.json'), exampleQuestions)
})

test('CSV handles UTF-8 BOM, quoted commas, escaped quotes and multiline content', () => {
  const csv = '\uFEFFtype,content,options,answers\r\nSingle,"Line 1,\n""Line 2""",Yes|No,A\r\nMultiple,Pick two,A|B|C,"A,C"\r\n'
  const result = parseChoiceImport(csv, 'bank.csv')
  assert.equal(result[0].content, 'Line 1,\n"Line 2"')
  assert.deepEqual(result[1].correctAnswers, [0, 2])
})

test('invalid row rejects the entire import', () => {
  for (const bad of [
    { ...exampleQuestions[0], type: 'Unsupported' },
    { ...exampleQuestions[0], correctAnswers: [0, 1] },
    { ...exampleQuestions[1], correctAnswers: [0] },
    { ...exampleQuestions[1], correctAnswers: [0, 0] },
    { ...exampleQuestions[1], correctAnswers: [-1, 9] },
    { ...exampleQuestions[0], options: ['A', 'A'] },
    { ...exampleQuestions[0], content: ' ' },
  ]) assert.throws(() => parseChoiceImport(JSON.stringify([exampleQuestions[0], bad]), 'bad.json'), /第 2/)
})

test('malformed CSV and invalid columns are rejected', () => {
  assert.throws(() => parseCsv('a,"unclosed'), /引号/)
  assert.throws(() => parseCsv('"quoted"extra'), /引号/)
  assert.throws(() => parseChoiceImport('type,content,answers\nSingle,Test,A', 'bad.csv'), /表头/)
  assert.throws(() => parseChoiceImport('type,content,options,answers\nSingle,Test,A|B,A,extra', 'bad.csv'), /四列/)
})

test('empty and oversized imports are rejected', () => {
  assert.throws(() => parseChoiceImport('[]', 'empty.json'))
  assert.throws(() => parseChoiceImport('{}', 'empty.json'))
  assert.throws(() => parseChoiceImport(JSON.stringify(Array(5001).fill(exampleQuestions[0])), 'large.json'))
})
