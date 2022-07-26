import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Text,
  Stack,
  Group,
  Input,
  NumberInput,
  Select,
  SimpleGrid,
  Slider,
  Textarea,
  TextInput,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiBackburger, mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeModel, ChallengeTag, ChallengeType } from '../../../../../../Api'
import {
  ChallengeTypeItem,
  ChallengeTypeLabelMap,
  ChallengeTagItem,
  ChallengeTagLabelMap,
} from '../../../../../../components/ChallengeItem'
import ScoreFunc from '../../../../../../components/admin/ScoreFunc'
import WithGameTab from '../../../../../../components/admin/WithGameTab'

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { data: challenge, mutate } = api.edit.useEditGetGameChallenge(numId, numCId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [challengeInfo, setChallengeInfo] = useState<ChallengeModel>({ ...challenge })
  const [disabled, setDisabled] = useState(false)

  const [minRate, setMinRate] = useState((challenge?.minScoreRate ?? 0.25) * 100)
  const [tag, setTag] = useState<ChallengeTag>(challenge?.tag ?? ChallengeTag.Misc)
  const [type, setType] = useState<ChallengeType>(challenge?.type ?? ChallengeType.StaticAttachment)
  const [currentAcceptCount, setCurrentAcceptCount] = useState(0)

  const modals = useModals()

  useEffect(() => {
    if (challenge) {
      setChallengeInfo({ ...challenge })
      setTag(challenge.tag)
      setType(challenge.type)
      setMinRate((challenge?.minScoreRate ?? 0.25) * 100)
      setCurrentAcceptCount(challenge.acceptedCount)
    }
  }, [challenge])

  const onUpdate = (challenge: ChallengeModel) => {
    if (challenge) {
      setDisabled(true)
      api.edit
        .editUpdateGameChallenge(numId, numCId, challenge)
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '题目已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          mutate(data.data)
          api.edit.mutateEditGetGameChallenges(numId)
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
        })
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  const onConfirmDelete = () => {
    api.edit
      .editRemoveGameChallenge(numId, numCId)
      .then(() => {
        showNotification({
          color: 'teal',
          message: '题目已删除',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        api.edit.mutateEditGetGameChallenges(numId)
        navigate(`/admin/games/${id}/challenges`)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <WithGameTab
      isLoading={!challenge}
      headProps={{ position: 'apart' }}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiBackburger} size={1} />}
            onClick={() => navigate(`/admin/games/${id}/challenges`)}
          >
            返回上级
          </Button>
          <Group position="right">
            <Button
              disabled={disabled}
              color="red"
              variant="outline"
              onClick={() =>
                modals.openConfirmModal({
                  title: `删除题目`,
                  children: <Text size="sm">你确定要删除题目 "{challengeInfo.title}" 吗？</Text>,
                  onConfirm: () => onConfirmDelete(),
                  centered: true,
                  labels: { confirm: '确认', cancel: '取消' },
                  confirmProps: { color: 'red' },
                })
              }
            >
              删除题目
            </Button>
            <Button
              disabled={disabled}
              onClick={() => navigate(`/admin/games/${numId}/challenges/${numCId}/flags`)}
            >
              编辑附件及 flag
            </Button>
            <Button
              disabled={disabled}
              onClick={() =>
                onUpdate({
                  ...challengeInfo,
                  tag: tag,
                  type: type,
                  minScoreRate: minRate / 100,
                })
              }
            >
              保存更改
            </Button>
          </Group>
        </>
      }
    >
      <Stack>
        <SimpleGrid cols={3}>
          <TextInput
            label="题目标题"
            disabled={disabled}
            value={challengeInfo.title ?? ''}
            required
            onChange={(e) => setChallengeInfo({ ...challengeInfo, title: e.target.value })}
          />
          <Select
            required
            label="题目类型"
            placeholder="Type"
            value={type}
            disabled={disabled}
            onChange={(e) => {
              setType(e as ChallengeType)
              setChallengeInfo({ ...challengeInfo, type: e as ChallengeType })
            }}
            itemComponent={ChallengeTypeItem}
            data={Object.entries(ChallengeType).map((type) => {
              const data = ChallengeTypeLabelMap.get(type[1])
              return { value: type[1], ...data }
            })}
          />
          <Select
            required
            label="题目标签"
            placeholder="Tag"
            value={tag}
            disabled={disabled}
            onChange={(e) => {
              setTag(e as ChallengeTag)
              setChallengeInfo({ ...challengeInfo, tag: e as ChallengeTag })
            }}
            itemComponent={ChallengeTagItem}
            data={Object.entries(ChallengeTag).map((tag) => {
              const data = ChallengeTagLabelMap.get(tag[1])
              return { value: tag[1], ...data }
            })}
          />
          <Stack spacing="sm">
            <Textarea
              label={
                <Group spacing="sm">
                  <Text size="sm">题目描述</Text>
                  <Text size="xs" color="dimmed">
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
                  <Text size="xs" color="dimmed">
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
          <Stack spacing="sm">
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
                  `最低分值: ${((value / 100) * (challengeInfo?.originalScore ?? 500)).toFixed(
                    0
                  )}pt`
                }
                disabled={disabled}
                value={minRate}
                marks={[
                  { value: 20, label: '20%' },
                  { value: 50, label: '50%' },
                  { value: 80, label: '80%' },
                ]}
                onChange={setMinRate}
                styles={(theme) => ({
                  label: {
                    background:
                      theme.colorScheme === 'dark' ? theme.colors.dark[4] : 'rgba(0, 0, 0, 0.8)',
                  },
                })}
              />
            </Input.Wrapper>
          </Stack>
          <ScoreFunc
            currentAcceptCount={currentAcceptCount}
            originalScore={challengeInfo?.originalScore ?? 500}
            minScoreRate={minRate / 100}
            difficulty={challengeInfo?.difficulty ?? 30}
          />
        </SimpleGrid>
        {(challengeInfo.type === ChallengeType.StaticContainer ||
          challengeInfo.type === ChallengeType.DynamicContainer) && (
          <>
            <TextInput
              label="容器镜像"
              disabled={disabled}
              value={challengeInfo.containerImage ?? ''}
              required
              onChange={(e) =>
                setChallengeInfo({ ...challengeInfo, containerImage: e.target.value })
              }
            />
            <SimpleGrid cols={3}>
              <NumberInput
                label="服务端口"
                min={1}
                max={65535}
                required
                disabled={disabled}
                stepHoldDelay={500}
                stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
                value={challengeInfo.containerExposePort ?? 1}
                onChange={(e) => setChallengeInfo({ ...challengeInfo, containerExposePort: e })}
              />
              <NumberInput
                label="CPU 数量限制"
                min={1}
                max={16}
                required
                disabled={disabled}
                stepHoldDelay={500}
                stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
                value={challengeInfo.cpuCount ?? 1}
                onChange={(e) => setChallengeInfo({ ...challengeInfo, cpuCount: e })}
              />
              <NumberInput
                label="内存限制 (MB)"
                min={64}
                max={8192}
                required
                disabled={disabled}
                stepHoldDelay={500}
                stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
                value={challengeInfo.memoryLimit ?? 1}
                onChange={(e) => setChallengeInfo({ ...challengeInfo, memoryLimit: e })}
              />
            </SimpleGrid>
          </>
        )}
      </Stack>
    </WithGameTab>
  )
}

export default GameChallengeEdit
