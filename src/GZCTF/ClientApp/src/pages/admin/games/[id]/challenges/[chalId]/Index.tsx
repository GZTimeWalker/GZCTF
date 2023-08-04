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
  Slider,
  Textarea,
  TextInput,
  Grid,
  Code,
  Switch,
  Title,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import {
  mdiCheck,
  mdiContentSaveOutline,
  mdiDatabaseEditOutline,
  mdiDeleteOutline,
  mdiEyeOutline,
  mdiKeyboardBackspace,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import HintList from '@Components/HintList'
import ChallengePreviewModal from '@Components/admin/ChallengePreviewModal'
import ScoreFunc from '@Components/admin/ScoreFunc'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import {
  ChallengeTypeItem,
  ChallengeTypeLabelMap,
  ChallengeTagItem,
  ChallengeTagLabelMap,
} from '@Utils/Shared'
import { OnceSWRConfig } from '@Utils/useConfig'
import { useEditChallenge } from '@Utils/useEdit'
import api, { ChallengeUpdateModel, ChallengeTag, ChallengeType, FileType } from '@Api'

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { challenge, mutate } = useEditChallenge(numId, numCId)

  const { mutate: mutateChals } = api.edit.useEditGetGameChallenges(numId, OnceSWRConfig)

  const [challengeInfo, setChallengeInfo] = useState<ChallengeUpdateModel>({ ...challenge })
  const [disabled, setDisabled] = useState(false)

  const [minRate, setMinRate] = useState((challenge?.minScoreRate ?? 0.25) * 100)
  const [tag, setTag] = useState<string | null>(challenge?.tag ?? ChallengeTag.Misc)
  const [type, setType] = useState<string | null>(challenge?.type ?? ChallengeType.StaticAttachment)
  const [currentAcceptCount, setCurrentAcceptCount] = useState(0)
  const [previewOpend, setPreviewOpend] = useState(false)

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

  const onUpdate = (challenge: ChallengeUpdateModel, noFeedback?: boolean) => {
    if (challenge) {
      setDisabled(true)
      return api.edit
        .editUpdateGameChallenge(numId, numCId, {
          ...challenge,
          isEnabled: undefined,
        })
        .then((data) => {
          if (!noFeedback) {
            showNotification({
              color: 'teal',
              message: '题目已更新',
              icon: <Icon path={mdiCheck} size={1} />,
            })
          }
          mutate(data.data)
          mutateChals()
        })
        .catch(showErrorNotification)
        .finally(() => {
          if (!noFeedback) {
            setDisabled(false)
          }
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
        })
        mutateChals((chals) => chals?.filter((chal) => chal.id !== numCId), { revalidate: false })
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
        })
        if (challenge) mutate({ ...challenge, testContainer: res.data })
      })
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  const onDestroyTestContainer = () => {
    api.edit
      .editDestroyTestContainer(numId, numCId)
      .then(() => {
        showNotification({
          color: 'teal',
          message: '实例已销毁',
          icon: <Icon path={mdiCheck} size={1} />,
        })
        if (challenge) mutate({ ...challenge, testContainer: undefined })
      })
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  const onToggleTestContainer = () => {
    if (!challenge) return

    setDisabled(true)
    onUpdate(
      {
        ...challengeInfo,
        tag: tag as ChallengeTag,
        minScoreRate: minRate / 100,
      },
      true
    )?.then(challenge?.testContainer ? onDestroyTestContainer : onCreateTestContainer)
  }

  return (
    <WithGameEditTab
      isLoading={!challenge}
      headProps={{ position: 'apart' }}
      head={
        <>
          <Group noWrap position="left">
            <Button
              leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
              onClick={() => navigate(`/admin/games/${id}/challenges`)}
            >
              返回上级
            </Button>
            <Title lineClamp={1} style={{ wordBreak: 'break-all' }}>
              # {challengeInfo?.title}
            </Title>
          </Group>
          <Group noWrap position="right">
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

                  labels: { confirm: '确认', cancel: '取消' },
                  confirmProps: { color: 'red' },
                })
              }
            >
              删除题目
            </Button>
            <Button
              disabled={disabled}
              leftIcon={<Icon path={mdiEyeOutline} size={1} />}
              onClick={() => setPreviewOpend(true)}
            >
              题目预览
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
        <Grid columns={3}>
          <Grid.Col span={1}>
            <TextInput
              label="题目标题"
              disabled={disabled}
              value={challengeInfo.title ?? ''}
              required
              onChange={(e) => setChallengeInfo({ ...challengeInfo, title: e.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={1}>
            <Select
              label={
                <Group spacing="sm">
                  <Text size="sm">题目类型</Text>
                  <Text size="xs" c="dimmed">
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
          </Grid.Col>
          <Grid.Col span={1}>
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
          </Grid.Col>
          <Grid.Col span={3}>
            <Textarea
              w="100%"
              label={
                <Group spacing="sm">
                  <Text size="sm">题目描述</Text>
                  <Text size="xs" c="dimmed">
                    支持 Markdown 语法
                  </Text>
                </Group>
              }
              value={challengeInfo?.content ?? ''}
              autosize
              disabled={disabled}
              minRows={5}
              maxRows={5}
              onChange={(e) => setChallengeInfo({ ...challengeInfo, content: e.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={1}>
            <Stack spacing="sm">
              <HintList
                label={
                  <Group spacing="sm">
                    <Text size="sm">题目提示</Text>
                    <Text size="xs" c="dimmed">
                      支持 Inline Markdown 语法
                    </Text>
                  </Group>
                }
                hints={challengeInfo?.hints ?? []}
                disabled={disabled}
                height={180}
                onChangeHint={(hints) => setChallengeInfo({ ...challengeInfo, hints })}
              />
            </Stack>
          </Grid.Col>
          <Grid.Col span={1}>
            <Stack spacing="sm">
              <NumberInput
                label="题目分值"
                min={0}
                required
                disabled={disabled}
                stepHoldDelay={500}
                stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
                value={challengeInfo?.originalScore ?? 500}
                onChange={(e) =>
                  e !== '' && setChallengeInfo({ ...challengeInfo, originalScore: e })
                }
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
                onChange={(e) => e !== '' && setChallengeInfo({ ...challengeInfo, difficulty: e })}
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
          </Grid.Col>
          <Grid.Col span={1}>
            <ScoreFunc
              currentAcceptCount={currentAcceptCount}
              originalScore={challengeInfo.originalScore ?? 500}
              minScoreRate={minRate / 100}
              difficulty={challengeInfo.difficulty ?? 30}
            />
          </Grid.Col>
        </Grid>
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
          <Grid columns={12}>
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
              <Group spacing={0} align="center" pt={22} h="100%">
                {challenge?.testContainer ? (
                  <Code
                    sx={(theme) => ({
                      backgroundColor: 'transparent',
                      fontSize: theme.fontSizes.sm,
                      fontWeight: 'bold',
                    })}
                    onClick={() => clipBoard.copy(challenge?.testContainer?.entry ?? '')}
                  >
                    {challenge?.testContainer?.entry ?? ''}
                  </Code>
                ) : (
                  <Text size="sm" fw={600} c="dimmed">
                    测试容器未开启
                  </Text>
                )}
              </Group>
            </Grid.Col>
            <Grid.Col span={2}>
              <NumberInput
                label="服务端口"
                description="容器内服务暴露的端口"
                min={1}
                max={65535}
                required
                disabled={disabled}
                stepHoldDelay={500}
                stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
                value={challengeInfo.containerExposePort ?? 1}
                onChange={(e) =>
                  e !== '' && setChallengeInfo({ ...challengeInfo, containerExposePort: e })
                }
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <NumberInput
                label="CPU 限制 (0.1 CPUs)"
                description="乘以 0.1 即为 CPU 核心数"
                min={1}
                max={1024}
                required
                disabled={disabled}
                stepHoldDelay={500}
                stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
                value={challengeInfo.cpuCount ?? 1}
                onChange={(e) => e !== '' && setChallengeInfo({ ...challengeInfo, cpuCount: e })}
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <NumberInput
                label="内存限制 (MB)"
                description="限制容器使用的 RAM"
                min={32}
                max={1048576}
                required
                disabled={disabled}
                stepHoldDelay={500}
                stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
                value={challengeInfo.memoryLimit ?? 32}
                onChange={(e) => e !== '' && setChallengeInfo({ ...challengeInfo, memoryLimit: e })}
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <NumberInput
                label="存储限制 (MB)"
                description="限制存储空间，含镜像大小"
                min={128}
                max={1048576}
                required
                disabled={disabled}
                stepHoldDelay={500}
                stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
                value={challengeInfo.storageLimit ?? 128}
                onChange={(e) =>
                  e !== '' && setChallengeInfo({ ...challengeInfo, storageLimit: e })
                }
              />
            </Grid.Col>
            <Grid.Col span={4} style={{ alignItems: 'center', display: 'flex' }}>
              <Switch
                disabled={disabled}
                checked={challengeInfo.privilegedContainer ?? false}
                label={SwitchLabel('特权容器', '以特权模式运行容器，Swarm 不受支持')}
                onChange={(e) =>
                  setChallengeInfo({ ...challengeInfo, privilegedContainer: e.target.checked })
                }
              />
            </Grid.Col>
          </Grid>
        )}
      </Stack>
      <ChallengePreviewModal
        challenge={challengeInfo}
        opened={previewOpend}
        onClose={() => setPreviewOpend(false)}
        withCloseButton={false}
        size="40%"
        type={challenge?.type ?? ChallengeType.StaticAttachment}
        tagData={
          ChallengeTagLabelMap.get((challengeInfo?.tag as ChallengeTag) ?? ChallengeTag.Misc)!
        }
        attachmentType={challenge?.attachment?.type ?? FileType.None}
      />
    </WithGameEditTab>
  )
}

export default GameChallengeEdit
