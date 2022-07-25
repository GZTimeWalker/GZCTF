import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Grid,
  Group,
  Select,
  Textarea,
  TextInput,
  Text,
  Stack,
  NumberInput,
  Slider,
  Input,
  SimpleGrid,
} from '@mantine/core'
import { mdiBackburger } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeModel, ChallengeTag, ChallengeType } from '../../../../../Api'
import {
  ChallengeTagItem,
  ChallengeTagLabelMap,
  ChallengeTypeItem,
  ChallengeTypeLabelMap,
} from '../../../../../components/ChallengeItem'
import ScoreFunc from '../../../../../components/admin/ScoreFunc'
import WithGameTab from '../../../../../components/admin/WithGameTab'

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { data: challenge, mutate } = api.edit.useEditGetGameChallenge(numId, numCId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [minRate, setMinRate] = useState((challenge?.minScoreRate ?? 0.25) * 100)

  // do not set minScoreRate directly, use setMinRate
  const [challengeInfo, setChallengeInfo] = useState<ChallengeModel>({ ...challenge })
  const [disabled, setDisabled] = useState(false)

  useEffect(() => {
    setChallengeInfo({ ...challenge })
    setMinRate((challenge?.minScoreRate ?? 0.25) * 100)
  }, [challenge])

  return (
    <WithGameTab
      isLoading={!challenge}
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate(`/admin/games/${id}/challenges`)}
        >
          返回上级
        </Button>
      }
    >
      <Grid grow>
        <Grid.Col span={4}>
          <TextInput
            label="题目标题"
            disabled={disabled}
            value={challengeInfo.title ?? ''}
            required
            onChange={(e) => setChallengeInfo({ ...challengeInfo, title: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={4}>
          <Select
            required
            label="题目类型"
            placeholder="Type"
            value={challengeInfo?.type}
            onChange={(e) => setChallengeInfo({ ...challengeInfo, type: e as ChallengeType })}
            itemComponent={ChallengeTypeItem}
            data={Object.entries(ChallengeType).map((type) => {
              const data = ChallengeTypeLabelMap.get(type[1])
              return { value: type[1], ...data }
            })}
          />
        </Grid.Col>
        <Grid.Col span={4}>
          <Select
            required
            label="题目标签"
            placeholder="Tag"
            value={challengeInfo?.tag}
            onChange={(e) => setChallengeInfo({ ...challengeInfo, tag: e as ChallengeTag })}
            itemComponent={ChallengeTagItem}
            data={Object.entries(ChallengeTag).map((tag) => {
              const data = ChallengeTagLabelMap.get(tag[1])
              return { value: tag[1], ...data }
            })}
          />
        </Grid.Col>
      </Grid>
      <SimpleGrid cols={3}>
        <Stack>
          <Textarea
            label={
              <Group spacing="sm">
                <Text size="sm">题目描述</Text>
                <Text size="xs" color="gray">
                  支持 markdown 语法
                </Text>
              </Group>
            }
            value={challengeInfo?.content ?? ''}
            style={{ width: '100%' }}
            autosize
            disabled={disabled}
            minRows={4}
            maxRows={4}
            onChange={(e) =>
              setChallengeInfo({ ...challengeInfo, content: e.target.value.replace(/\n/g, '') })
            }
          />
          <Textarea
            label={
              <Group spacing="sm">
                <Text size="sm">题目提示</Text>
                <Text size="xs" color="gray">
                  请以分号 ; 分隔每个提示
                </Text>
              </Group>
            }
            value={challengeInfo.hints ?? ''}
            style={{ width: '100%' }}
            autosize
            disabled={disabled}
            minRows={1}
            maxRows={1}
            onChange={(e) => setChallengeInfo({ ...challengeInfo, hints: e.target.value })}
          />
        </Stack>
        <Stack>
          <NumberInput
            label="题目分值"
            min={0}
            required
            disabled={disabled}
            stepHoldDelay={500}
            stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
            value={challengeInfo?.originalScore ?? 500}
            onChange={(e) => setChallengeInfo({ ...challengeInfo, originalScore: e })}
          />
          <NumberInput
            label="难度系数"
            precision={1}
            step={0.2}
            min={0.1}
            required
            disabled={disabled}
            value={challengeInfo?.difficulty ?? 100}
            stepHoldDelay={500}
            stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
            onChange={(e) => setChallengeInfo({ ...challengeInfo, difficulty: e })}
          />
          <Input.Wrapper label="题目最低分值比例" required>
            <Slider
              label={(value) =>
                `最低分值: ${((value / 100) * (challengeInfo?.originalScore ?? 500)).toFixed(0)}pt`
              }
              value={minRate}
              marks={[
                { value: 20, label: '20%' },
                { value: 50, label: '50%' },
                { value: 80, label: '80%' },
              ]}
              onChange={setMinRate}
            />
          </Input.Wrapper>
        </Stack>
        <ScoreFunc
          originalScore={challengeInfo?.originalScore ?? 500}
          minScoreRate={minRate / 100}
          difficulty={challengeInfo?.difficulty ?? 30}
        />
      </SimpleGrid>
    </WithGameTab>
  )
}

export default GameChallengeEdit
