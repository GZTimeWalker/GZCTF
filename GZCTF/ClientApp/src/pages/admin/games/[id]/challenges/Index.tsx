import { Dispatch, FC, SetStateAction, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Center,
  Group,
  ScrollArea,
  Select,
  SimpleGrid,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiKeyboardBackspace, mdiCheck, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import ChallengeCreateModal from '@Components/admin/ChallengeCreateModal'
import ChallengeEditCard from '@Components/admin/ChallengeEditCard'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ChallengeTagItem, ChallengeTagLabelMap } from '@Utils/ChallengeItem'
import api, { ChallengeInfoModel, ChallengeTag } from '@Api'

const GameChallengeEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const navigate = useNavigate()
  const [createOpened, setCreateOpened] = useState(false)
  const [category, setCategory] = useState<ChallengeTag | null>(null)

  const { data: challenges, mutate } = api.edit.useEditGetGameChallenges(numId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const filteredChallenges =
    category && challenges ? challenges?.filter((c) => c.tag === category) : challenges
  filteredChallenges?.sort((a, b) => ((a.tag ?? '') > (b.tag ?? '') ? -1 : 1))

  const modals = useModals()
  const onToggle = (
    challenge: ChallengeInfoModel,
    setDisabled: Dispatch<SetStateAction<boolean>>
  ) => {
    const op = challenge.isEnabled ? '禁用' : '启用'
    modals.openConfirmModal({
      title: `${op}题目`,
      children: (
        <Text size="sm">
          你确定要{op}题目 "{challenge.title}" 吗？
        </Text>
      ),
      onConfirm: () => onConfirmToggle(challenge, setDisabled),
      centered: true,
      labels: { confirm: '确认', cancel: '取消' },
      confirmProps: { color: 'orange' },
    })
  }

  const onConfirmToggle = (
    challenge: ChallengeInfoModel,
    setDisabled: Dispatch<SetStateAction<boolean>>
  ) => {
    const numId = parseInt(id ?? '-1')
    setDisabled(true)
    api.edit
      .editUpdateGameChallenge(numId, challenge.id!, {
        isEnabled: !challenge.isEnabled,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          message: '题目状态更新成功',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        mutate(
          challenges?.map((c) =>
            c.id === challenge.id ? { ...c, isEnabled: !challenge.isEnabled } : c
          )
        )
      })
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <WithGameEditTab
      headProps={{ position: 'apart' }}
      isLoading={!challenges}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            返回上级
          </Button>
          <Group style={{ width: 'calc(100% - 9rem)' }} position="apart">
            <Select
              placeholder="全部题目"
              clearable
              searchable
              nothingFound="没有找到标签"
              clearButtonLabel="显示全部"
              value={category}
              onChange={(value: ChallengeTag) => setCategory(value)}
              itemComponent={ChallengeTagItem}
              data={Object.entries(ChallengeTag).map((tag) => {
                const data = ChallengeTagLabelMap.get(tag[1])
                return { value: tag[1], ...data }
              })}
            />
            <Button
              style={{ marginRight: '18px' }}
              leftIcon={<Icon path={mdiPlus} size={1} />}
              onClick={() => setCreateOpened(true)}
            >
              新建题目
            </Button>
          </Group>
        </>
      }
    >
      <ScrollArea
        style={{ height: 'calc(100vh - 180px)', position: 'relative' }}
        offsetScrollbars
        type="auto"
      >
        {!filteredChallenges || filteredChallenges.length === 0 ? (
          <Center style={{ height: 'calc(100vh - 200px)' }}>
            <Stack spacing={0}>
              <Title order={2}>Ouch! 这个比赛还没有题目</Title>
              <Text>点击右上角创建第一个题目</Text>
            </Stack>
          </Center>
        ) : (
          <SimpleGrid
            cols={2}
            pr={6}
            breakpoints={[
              { maxWidth: 3600, cols: 2, spacing: 'sm' },
              { maxWidth: 1800, cols: 1, spacing: 'sm' },
            ]}
          >
            {filteredChallenges &&
              filteredChallenges.map((challenge) => (
                <ChallengeEditCard key={challenge.id} challenge={challenge} onToggle={onToggle} />
              ))}
          </SimpleGrid>
        )}
      </ScrollArea>
      <ChallengeCreateModal
        title="新建题目"
        centered
        size="30%"
        opened={createOpened}
        onClose={() => setCreateOpened(false)}
        onAddChallenge={(challenge) => mutate([challenge, ...(challenges ?? [])])}
      />
    </WithGameEditTab>
  )
}

export default GameChallengeEdit
