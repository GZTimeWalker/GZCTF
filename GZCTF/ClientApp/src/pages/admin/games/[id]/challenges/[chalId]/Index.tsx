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
  Grid,
  Code,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import {
  mdiCheck,
  mdiContentSaveOutline,
  mdiDatabaseEditOutline,
  mdiDeleteOutline,
  mdiKeyboardBackspace,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import HintList from '@Components/HintList'
import ScoreFunc from '@Components/admin/ScoreFunc'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import {
  ChallengeTypeItem,
  ChallengeTypeLabelMap,
  ChallengeTagItem,
  ChallengeTagLabelMap,
} from '@Utils/ChallengeItem'
import api, { ChallengeUpdateModel, ChallengeTag, ChallengeType } from '@Api'

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { data: challenge, mutate } = api.edit.useEditGetGameChallenge(numId, numCId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [challengeInfo, setChallengeInfo] = useState<ChallengeUpdateModel>({ ...challenge })
  const [disabled, setDisabled] = useState(false)

  const [minRate, setMinRate] = useState((challenge?.minScoreRate ?? 0.25) * 100)
  const [tag, setTag] = useState<string | null>(challenge?.tag ?? ChallengeTag.Misc)
  const [type, setType] = useState<string | null>(challenge?.type ?? ChallengeType.StaticAttachment)
  const [currentAcceptCount, setCurrentAcceptCount] = useState(0)

  const modals = useModals()
  const clipBoard = useClipboard()

  useEffect(() => {
    if (challenge) {
      setChallengeInfo({ ...challenge })
      setTag(challenge.tag)
      setType(challenge.type)
      setMinRate((challenge?.minScoreRate ?? 0.25) * 100)
      setCurrentAcceptCount(challenge.acceptedCount)
    }
  }, [challenge])

  const onUpdate = (challenge: ChallengeUpdateModel) => {
    if (challenge) {
      setDisabled(true)
      return api.edit
        .editUpdateGameChallenge(numId, numCId, {
          ...challenge,
          isEnabled: undefined,
        })
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
        .catch(showErrorNotification)
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
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  const onCreateTestContainer = () => {
    api.edit
      .editCreateTestContainer(numId, numCId)
      .then((res) => {
        showNotification({
          color: 'teal',
          message: '实例已创建',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        if (challenge) mutate({ ...challenge, testContainer: res.data })
      })
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  const onToggleTestContainer = () => {
    if (!challenge) return

    setDisabled(true)
    if (!challenge?.testContainer) {
      if (
        challenge.containerImage !== challengeInfo.containerImage ||
        challenge.containerExposePort !== challengeInfo.containerExposePort
      )
        onUpdate(challengeInfo)?.then(onCreateTestContainer)
      else onCreateTestContainer()
    } else {
      api.edit
        .editDestroyTestContainer(numId, numCId)
        .then(() => {
          showNotification({
            color: 'teal',
            message: '实例已销毁',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          mutate({ ...challenge, testContainer: undefined })
        })
        .catch(showErrorNotification)
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  return (
    <WithGameEditTab
      isLoading={!challenge}
      headProps={{ position: 'apart' }}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
            onClick={() => navigate(`/admin/games/${id}/challenges`)}
          >
            返回上级
          </Button>
          <Group position="right">
            <Button
              disabled={disabled}
              color="red"
              leftIcon={<Icon path={mdiDeleteOutline} size={1} />}
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
              leftIcon={<Icon path={mdiDatabaseEditOutline} size={1} />}
              onClick={() => navigate(`/admin/games/${numId}/challenges/${numCId}/flags`)}
            >
              编辑附件及 flag
            </Button>
            <Button
              disabled={disabled}
              leftIcon={<Icon path={mdiContentSaveOutline} size={1} />}
              onClick={() =>
                onUpdate({
                  ...challengeInfo,
                  tag: tag as ChallengeTag,
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
            label={
              <Group spacing="sm">
                <Text size="sm">题目类型</Text>
                <Text size="xs" color="dimmed">
                  创建后不可更改
                </Text>
              </Group>
            }
            placeholder="Type"
            value={type}
            disabled={disabled}
            readOnly
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
              setTag(e)
              setChallengeInfo({ ...challengeInfo, tag: e as ChallengeTag })
            }}
            itemComponent={ChallengeTagItem}
            data={Object.entries(ChallengeTag).map((tag) => {
              const data = ChallengeTagLabelMap.get(tag[1])
              return { value: tag[1], ...data }
            })}
          />
        </SimpleGrid>
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
          minRows={3}
          maxRows={3}
          onChange={(e) => setChallengeInfo({ ...challengeInfo, content: e.target.value })}
        />
        <SimpleGrid cols={3}>
          <Stack spacing="sm">
            <HintList
              label="题目提示"
              hints={challengeInfo?.hints ?? []}
              disabled={disabled}
              height={180}
              onChangeHint={(hints) => setChallengeInfo({ ...challengeInfo, hints })}
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
                  )}pts`
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
            originalScore={challengeInfo.originalScore ?? 500}
            minScoreRate={minRate / 100}
            difficulty={challengeInfo.difficulty ?? 30}
          />
        </SimpleGrid>
        {type === ChallengeType.DynamicAttachment && (
          <TextInput
            label="全局附件名"
            description="所有动态附件均会以此文件名下载"
            disabled={disabled}
            value={challengeInfo.fileName ?? 'attachment'}
            onChange={(e) => setChallengeInfo({ ...challengeInfo, fileName: e.target.value })}
          />
        )}
        {(type === ChallengeType.StaticContainer || type === ChallengeType.DynamicContainer) && (
          <Grid>
            <Grid.Col span={8}>
              <TextInput
                label="容器镜像"
                disabled={disabled}
                value={challengeInfo.containerImage ?? ''}
                required
                rightSectionWidth={122}
                rightSection={
                  <Button
                    color={challenge?.testContainer ? 'orange' : 'brand'}
                    disabled={disabled}
                    onClick={onToggleTestContainer}
                  >
                    {challenge?.testContainer ? '关闭' : '开启'}测试容器
                  </Button>
                }
                onChange={(e) =>
                  setChallengeInfo({ ...challengeInfo, containerImage: e.target.value })
                }
              />
            </Grid.Col>
            <Grid.Col span={4}>
              <Group spacing={0} align="center" pt={22} style={{ height: '100%' }}>
                {challenge?.testContainer ? (
                  <>
                    <Text size="sm" weight={600}>
                      测试容器访问入口：
                    </Text>
                    <Code
                      sx={(theme) => ({
                        backgroundColor: 'transparent',
                        fontSize: theme.fontSizes.sm,
                      })}
                      onClick={() => clipBoard.copy(challenge?.testContainer?.entry ?? '')}
                    >
                      {challenge?.testContainer?.entry ?? ''}
                    </Code>
                  </>
                ) : (
                  <Text size="sm" weight={600} color="dimmed">
                    测试容器未开启
                  </Text>
                )}
              </Group>
            </Grid.Col>
            <Grid.Col span={4}>
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
            </Grid.Col>
            <Grid.Col span={4}>
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
            </Grid.Col>
            <Grid.Col span={4}>
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
            </Grid.Col>
          </Grid>
        )}
      </Stack>
    </WithGameEditTab>
  )
}

export default GameChallengeEdit
