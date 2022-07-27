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
} from '@mantine/core'
import { DatePicker, TimeInput } from '@mantine/dates'
import { Dropzone } from '@mantine/dropzone'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiBackburger, mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { GameInfoModel } from '../../../../Api'
import WithGameTab from '../../../../components/admin/WithGameTab'

const GameInfoEdit: FC = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const [game, setGame] = useState<GameInfoModel>()

  const [disabled, setDisabled] = useState(false)
  const [start, setStart] = useInputState(new Date())
  const [end, setEnd] = useInputState(new Date())

  useEffect(() => {
    const numId = parseInt(id ?? '-1')

    if (numId < 0) {
      showNotification({
        color: 'red',
        message: `比赛 Id 错误：${id}`,
        icon: <Icon path={mdiClose} size={1} />,
      })
      navigate('/admin/games')
      return
    }

    api.edit
      .editGetGame(numId)
      .then((data) => {
        setGame(data.data)
        setStart(new Date(data.data.start))
        setEnd(new Date(data.data.end))
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

        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
  }, [id])

  const onUpdatePoster = (file: File | undefined) => {
    if (game && file) {
      api.edit
        .editUpdateGamePoster(game.id!, { file })
        .then((res) => {
          showNotification({
            color: 'teal',
            message: '修改头像成功',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          setGame({ ...game, poster: res.data })
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
        })
    }
  }

  const onUpdateInfo = () => {
    if (game && game.title) {
      setDisabled(true)
      api.edit
        .editUpdateGame(game.id!, {
          ...game,
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

  return (
    <WithGameTab
      headProps={{ position: 'apart' }}
      isLoading={!game}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiBackburger} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            返回上级
          </Button>
          <Button disabled={disabled} onClick={onUpdateInfo}>
            保存更改
          </Button>
        </>
      }
    >
      <Grid grow>
        <Grid.Col span={8}>
          <TextInput
            label="比赛标题"
            disabled={disabled}
            value={game?.title}
            required
            onChange={(e) => game && setGame({ ...game, title: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={4}>
          <NumberInput
            label="报名队伍人数限制"
            disabled={disabled}
            min={0}
            value={game?.teamMemberCountLimit}
            onChange={(e) => game && setGame({ ...game, teamMemberCountLimit: e })}
          />
        </Grid.Col>
      </Grid>
      <Group grow position="apart">
        <DatePicker
          label="开始日期"
          disabled={disabled}
          placeholder="Start Date"
          value={start}
          clearable={false}
          onChange={(e) => setStart(e)}
          required
        />
        <TimeInput
          label="开始时间"
          disabled={disabled}
          placeholder="Start Time"
          value={start}
          onChange={(e) => setStart(e)}
          withSeconds
          required
        />
        <DatePicker
          label="结束日期"
          disabled={disabled}
          minDate={start}
          placeholder="End time"
          value={end}
          clearable={false}
          onChange={(e) => setEnd(e)}
          error={end < start}
          required
        />
        <TimeInput
          label="结束时间"
          disabled={disabled}
          placeholder="End time"
          value={end}
          onChange={(e) => setEnd(e)}
          error={end < start}
          withSeconds
          required
        />
      </Group>
      <Grid grow>
        <Grid.Col span={8}>
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
                })
              }}
              maxSize={3 * 1024 * 1024}
              accept={['image/png', 'image/gif', 'image/jpeg']}
              disabled={disabled}
              styles={{
                root: {
                  height: '110px',
                  padding: game?.poster ? '0' : '16px',
                },
              }}
            >
              <Center style={{ pointerEvents: 'none' }}>
                {game?.poster ? (
                  <Image height="105px" fit="contain" src={game.poster} />
                ) : (
                  <Center style={{ height: '74px' }}>
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
        minRows={6}
        maxRows={7}
        onChange={(e) => game && setGame({ ...game, content: e.target.value })}
      />
    </WithGameTab>
  )
}

export default GameInfoEdit
