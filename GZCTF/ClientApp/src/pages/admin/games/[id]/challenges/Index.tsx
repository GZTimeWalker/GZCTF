import { Dispatch, FC, SetStateAction, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Group, ScrollArea, Select, SimpleGrid, Text } from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiBackburger, mdiCheck, mdiClose, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeInfoModel, ChallengeTag } from '../../../../../Api'
import { ChallengeTagItem, ChallengeTagLabelMap } from '../../../../../components/ChallengeItem'
import ChallengeCreateModal from '../../../../../components/admin/ChallengeCreateModal'
import ChallengeEditCard from '../../../../../components/admin/ChallengeEditCard'
import WithGameTab from '../../../../../components/admin/WithGameTab'

const GameChallengeEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const navigate = useNavigate()
  const [createOpened, setCreateOpened] = useState(false)
  const [categray, setCategray] = useState<ChallengeTag | null>(null)

  const { data: challenges, mutate } = api.edit.useEditGetGameChallenges(numId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const filteredChallenges =
    categray && challenges ? challenges?.filter((c) => c.tag === categray) : challenges

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
        mutate([
          ...(challenges?.filter((c) => c.id != challenge.id) ?? []),
          { ...challenge, isEnabled: !challenge.isEnabled },
        ])
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
          disallowClose: true,
        })
      })
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <WithGameTab
      headProps={{ position: 'apart' }}
      isLoading={!challenges}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiBackburger} size={1} />}
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
              value={categray}
              onChange={(value) => setCategray(value as ChallengeTag)}
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
      <ScrollArea style={{ height: 'calc(100vh - 250px)' }} offsetScrollbars type="auto">
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
      </ScrollArea>

      <ChallengeCreateModal
        title="新建题目"
        centered
        size="30%"
        opened={createOpened}
        onClose={() => setCreateOpened(false)}
        onAddChallenge={(challenge) => mutate([challenge, ...(challenges ?? [])])}
      />
    </WithGameTab>
  )
}

export default GameChallengeEdit
