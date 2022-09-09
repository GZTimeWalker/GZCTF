import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Center,
  Grid,
  Group,
  Input,
  NumberInput,
  Stack,
  Textarea,
  TextInput,
  Image,
  Text,
  MultiSelect,
  ActionIcon,
  Switch,
  PasswordInput,
} from '@mantine/core'
import { DatePicker, TimeInput } from '@mantine/dates'
import { Dropzone } from '@mantine/dropzone'
import { useClipboard, useInputState } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import {
  mdiKeyboardBackspace,
  mdiCheck,
  mdiClose,
  mdiContentSaveOutline,
  mdiRefresh,
  mdiDeleteOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ACCEPT_IMAGE_MIME_TYPE } from '@Utils/ThemeOverride'
import api, { GameInfoModel } from '@Api'

const GenerateRandomCode = () => {
  const chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
  let code = ''
  for (let i = 0; i < 24; i++) {
    code += chars[Math.floor(Math.random() * chars.length)]
  }
  return code
}

const GameInfoEdit: FC = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const [game, setGame] = useState<GameInfoModel>()

  const [disabled, setDisabled] = useState(false)
  const [organizations, setOrganizations] = useState<string[]>([])
  const [start, setStart] = useInputState(dayjs())
  const [end, setEnd] = useInputState(dayjs())

  const modals = useModals()
  const clipboard = useClipboard()

  useEffect(() => {
    const numId = parseInt(id ?? '-1')

    if (numId < 0) {
      showNotification({
        color: 'red',
        message: `比赛 Id 错误：${id}`,
        icon: <Icon path={mdiClose} size={1} />,
        disallowClose: true,
      })
      navigate('/admin/games')
      return
    }

    api.edit
      .editGetGame(numId)
      .then((data) => {
        setGame(data.data)
        setOrganizations(data.data.organizations || [])
        setStart(dayjs(data.data.start))
        setEnd(dayjs(data.data.end))
      })
      .catch((err) => {
        if (err.status === 404) {
          showNotification({
            color: 'red',
            message: `比赛未找到：${id}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
          navigate('/admin/games')
        }

        showErrorNotification(err)
      })
  }, [id])

  const onUpdatePoster = (file: File | undefined) => {
    if (game && file) {
      api.edit
        .editUpdateGamePoster(game.id!, { file })
        .then((res) => {
          showNotification({
            color: 'teal',
            message: '成功修改比赛海报',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          setGame({ ...game, poster: res.data })
        })
        .catch(showErrorNotification)
    }
  }

  const onUpdateInfo = () => {
    if (game && game.title) {
      setDisabled(true)
      api.edit
        .editUpdateGame(game.id!, {
          ...game,
          inviteCode: game.inviteCode?.length ?? 0 > 6 ? game.inviteCode : null,
          start: start.toJSON(),
          end: end.toJSON(),
        })
        .then(() => {
          showNotification({
            color: 'teal',
            message: '比赛信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          api.game.mutateGameGamesAll()
        })
        .catch(showErrorNotification)
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  const onConfirmDelete = () => {
    if (game) {
      api.edit
        .editDeleteGame(game.id!)
        .then(() => {
          showNotification({
            color: 'teal',
            message: '比赛已删除',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          navigate('/admin/games')
        })
        .catch(showErrorNotification)
    }
  }

  return (
    <WithGameEditTab
      headProps={{ position: 'apart' }}
      isLoading={!game}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
            onClick={() => navigate('/admin/games')}
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
                  title: `删除比赛`,
                  children: <Text size="sm">你确定要删除比赛 "{game?.title}" 吗？</Text>,
                  onConfirm: () => onConfirmDelete(),
                  centered: true,
                  labels: { confirm: '确认', cancel: '取消' },
                  confirmProps: { color: 'red' },
                })
              }
            >
              删除比赛
            </Button>
            <Button
              leftIcon={<Icon path={mdiContentSaveOutline} size={1} />}
              disabled={disabled}
              onClick={onUpdateInfo}
            >
              保存更改
            </Button>
          </Group>
        </>
      }
    >
      <Grid grow>
        <Grid.Col span={6}>
          <TextInput
            label="比赛标题"
            disabled={disabled}
            value={game?.title}
            required
            onChange={(e) => game && setGame({ ...game, title: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={3}>
          <NumberInput
            label="队伍人数限制"
            disabled={disabled}
            min={0}
            value={game?.teamMemberCountLimit}
            onChange={(e) => game && setGame({ ...game, teamMemberCountLimit: e })}
          />
        </Grid.Col>
        <Grid.Col span={3}>
          <NumberInput
            label="队伍容器数量限制"
            disabled={disabled}
            min={1}
            value={game?.containerCountLimit}
            onChange={(e) => game && setGame({ ...game, containerCountLimit: e })}
          />
        </Grid.Col>
      </Grid>
      <Group grow position="apart">
        <DatePicker
          label="开始日期"
          placeholder="Start Date"
          value={start.toDate()}
          disabled={disabled}
          clearable={false}
          onChange={(e) => {
            const newDate = dayjs(e)
              .hour(start.hour())
              .minute(start.minute())
              .second(start.second())
            setStart(newDate)
            if (newDate && end < newDate) {
              setEnd(newDate.add(2, 'h'))
            }
          }}
          required
        />
        <TimeInput
          label="开始时间"
          disabled={disabled}
          placeholder="Start Time"
          value={start.toDate()}
          onChange={(e) => {
            const newDate = dayjs(e).date(start.date()).month(start.month()).year(start.year())
            setStart(newDate)
            if (newDate && end < newDate) {
              setEnd(newDate.add(2, 'h'))
            }
          }}
          withSeconds
          required
        />
        <DatePicker
          label="结束日期"
          disabled={disabled}
          minDate={start.toDate()}
          placeholder="End time"
          value={end.toDate()}
          clearable={false}
          onChange={(e) => {
            const newDate = dayjs(e).hour(end.hour()).minute(end.minute()).second(end.second())
            setEnd(newDate)
          }}
          error={end < start}
          required
        />
        <TimeInput
          label="结束时间"
          disabled={disabled}
          placeholder="End time"
          value={end.toDate()}
          onChange={(e) => {
            const newDate = dayjs(e).date(end.date()).month(end.month()).year(end.year())
            setEnd(newDate)
          }}
          error={end < start}
          withSeconds
          required
        />
      </Group>
      <Grid grow>
        <Grid.Col span={6}>
          <Stack>
            <PasswordInput
              value={game?.publicKey || ''}
              label={
                <Group spacing="sm">
                  <Text size="sm">比赛签名公钥</Text>
                  <Text size="xs" color="dimmed">
                    用于验证队伍 Token
                  </Text>
                </Group>
              }
              readOnly
              onClick={() => {
                clipboard.copy(game?.publicKey || '')
                showNotification({
                  color: 'teal',
                  message: '公钥已复制到剪贴板',
                  icon: <Icon path={mdiCheck} size={1} />,
                  disallowClose: true,
                })
              }}
            />
            <Textarea
              label="比赛简介"
              value={game?.summary}
              style={{ width: '100%' }}
              autosize
              disabled={disabled}
              minRows={4}
              maxRows={4}
              onChange={(e) => game && setGame({ ...game, summary: e.target.value })}
            />
          </Stack>
        </Grid.Col>
        <Grid.Col span={6}>
          <Stack>
            <Group grow>
              <TextInput
                label={
                  <Group spacing="sm">
                    <Text size="sm">邀请码</Text>
                    <Text size="xs" color="dimmed">
                      留空以不启用
                    </Text>
                  </Group>
                }
                value={game?.inviteCode || ''}
                disabled={disabled}
                onChange={(e) => game && setGame({ ...game, inviteCode: e.target.value })}
                rightSection={
                  <ActionIcon
                    onClick={() => game && setGame({ ...game, inviteCode: GenerateRandomCode() })}
                  >
                    <Icon path={mdiRefresh} size={1} />
                  </ActionIcon>
                }
              />
              <Switch
                style={{ marginTop: '1rem' }}
                disabled={disabled}
                checked={game?.acceptWithoutReview ?? false}
                label={SwitchLabel('队伍报名免审核', '队伍报名后直接设置为 Accept 状态')}
                onChange={(e) =>
                  game && setGame({ ...game, acceptWithoutReview: e.target.checked })
                }
              />
            </Group>
            <MultiSelect
              label={
                <Group spacing="sm">
                  <Text size="sm">参赛可选组织列表</Text>
                  <Text size="xs" color="dimmed">
                    添加参赛组织以开启分组榜单
                  </Text>
                </Group>
              }
              searchable
              creatable
              disabled={disabled}
              placeholder="无指定可选参赛组织，将允许无组织队伍参赛"
              maxDropdownHeight={300}
              value={game?.organizations ?? []}
              styles={{
                input: {
                  minHeight: 110,
                  maxHeight: 110,
                },
              }}
              onChange={(e) => game && setGame({ ...game, organizations: e })}
              data={organizations.map((o) => ({ value: o, label: o })) || []}
              getCreateLabel={(query) => `+ 添加组织 "${query}"`}
              onCreate={(query) => {
                const item = { value: query, label: query }
                setOrganizations([...organizations, query])
                return item
              }}
            />
          </Stack>
        </Grid.Col>
      </Grid>
      <Grid grow>
        <Grid.Col span={8}>
          <Textarea
            label={
              <Group spacing="sm">
                <Text size="sm">比赛详情</Text>
                <Text size="xs" color="dimmed">
                  支持 markdown 语法
                </Text>
              </Group>
            }
            value={game?.content}
            style={{ width: '100%' }}
            autosize
            disabled={disabled}
            minRows={8}
            maxRows={8}
            onChange={(e) => game && setGame({ ...game, content: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={4}>
          <Input.Wrapper label="比赛海报">
            <Dropzone
              onDrop={(files) => onUpdatePoster(files[0])}
              onReject={() => {
                showNotification({
                  color: 'red',
                  title: '文件获取失败',
                  message: '请检查文件格式和大小',
                  icon: <Icon path={mdiClose} size={1} />,
                  disallowClose: true,
                })
              }}
              maxSize={3 * 1024 * 1024}
              accept={ACCEPT_IMAGE_MIME_TYPE}
              disabled={disabled}
              styles={{
                root: {
                  height: '198px',
                  padding: game?.poster ? '0' : '16px',
                },
              }}
            >
              <Center style={{ pointerEvents: 'none' }}>
                {game?.poster ? (
                  <Image height="195px" fit="contain" src={game.poster} />
                ) : (
                  <Center style={{ height: '160px' }}>
                    <Stack spacing={0}>
                      <Text size="xl" inline>
                        拖放图片或点击此处以选择海报
                      </Text>
                      <Text size="sm" color="dimmed" inline mt={7}>
                        请选择小于 3MB 的图片
                      </Text>
                    </Stack>
                  </Center>
                )}
              </Center>
            </Dropzone>
          </Input.Wrapper>
        </Grid.Col>
      </Grid>
    </WithGameEditTab>
  )
}

export default GameInfoEdit
