import {
  Button,
  Center,
  ComboboxItem,
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
import { mdiCheck, mdiHexagonSlice6, mdiPlus, mdiRefresh } from '@mdi/js'
import { Icon } from '@mdi/react'
import { Dispatch, FC, SetStateAction, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import BloodBonusModel from '@Components/admin/BloodBonusModel'
import ChallengeCreateModal from '@Components/admin/ChallengeCreateModal'
import ChallengeEditCard from '@Components/admin/ChallengeEditCard'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiHelper'
import { ChallengeTagItem, useChallengeTagLabelMap } from '@Utils/Shared'
import { useEditChallenges } from '@Utils/useEdit'
import api, { ChallengeInfoModel, ChallengeTag } from '@Api'

const GameChallengeEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [createOpened, setCreateOpened] = useState(false)
  const [bonusOpened, setBonusOpened] = useState(false)
  const [category, setCategory] = useState<ChallengeTag | null>(null)
  const challengeTagLabelMap = useChallengeTagLabelMap()
  const [disabled, setDisabled] = useState(false)

  const { t } = useTranslation()

  const { challenges, mutate } = useEditChallenges(numId)

  const filteredChallenges =
    category && challenges ? challenges?.filter((c) => c.tag === category) : challenges

  const modals = useModals()

  const onToggle = (
    challenge: ChallengeInfoModel,
    setDisabled: Dispatch<SetStateAction<boolean>>
  ) => {
    modals.openConfirmModal({
      title: challenge.isEnabled
        ? t('admin.button.challenges.disable')
        : t('admin.button.challenges.enable'),
      children: (
        <Text size="sm">
          {challenge.isEnabled
            ? t('admin.content.games.challenges.disable', { name: challenge.title })
            : t('admin.content.games.challenges.enable', { name: challenge.title })}
        </Text>
      ),
      onConfirm: () => onConfirmToggle(challenge, setDisabled),
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
          message: t('admin.notification.games.challenges.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutate(
          challenges?.map((c) =>
            c.id === challenge.id ? { ...c, isEnabled: !challenge.isEnabled } : c
          )
        )
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  const onUpdateAcceptCount = () => {
    if (!numId) return

    setDisabled(true)
    api.edit
      .editUpdateGameChallengesAcceptedCount(numId)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.info.accept_count_updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutate()
      })
      .catch(() => showErrorNotification(t('common.error.try_later'), t))
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <WithGameEditTab
      headProps={{ justify: 'apart' }}
      isLoading={!challenges}
      head={
        <>
          <Select
            placeholder={t('admin.content.show_all')}
            clearable
            searchable
            w="16rem"
            value={category}
            nothingFoundMessage={t('admin.content.nothing_found')}
            onChange={(value) => setCategory(value as ChallengeTag | null)}
            renderOption={ChallengeTagItem}
            data={Object.entries(ChallengeTag).map((tag) => {
              const data = challengeTagLabelMap.get(tag[1])
              return { value: tag[1], label: data?.name, ...data } as ComboboxItem
            })}
          />
          <Group justify="right">
            <Button
              leftSection={<Icon path={mdiRefresh} size={1} />}
              disabled={disabled}
              onClick={onUpdateAcceptCount}
            >
              {t('admin.button.challenges.update_accept_count')}
            </Button>
            <Button
              leftSection={<Icon path={mdiHexagonSlice6} size={1} />}
              onClick={() => setBonusOpened(true)}
            >
              {t('admin.button.challenges.bonus')}
            </Button>
            <Button
              mr="18px"
              leftSection={<Icon path={mdiPlus} size={1} />}
              onClick={() => setCreateOpened(true)}
            >
              {t('admin.button.challenges.new')}
            </Button>
          </Group>
        </>
      }
    >
      <ScrollArea h="calc(100vh - 180px)" pos="relative" offsetScrollbars type="auto">
        {!filteredChallenges || filteredChallenges.length === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack gap={0}>
              <Title order={2}>{t('admin.content.games.challenges.empty.title')}</Title>
              <Text>{t('admin.content.games.challenges.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <SimpleGrid
            pr={6}
            cols={{ base: 2, w18: 3, w24: 4, w30: 5, w36: 6, w42: 7, w48: 8 }}
            spacing="sm"
          >
            {filteredChallenges &&
              filteredChallenges.map((challenge) => (
                <ChallengeEditCard key={challenge.id} challenge={challenge} onToggle={onToggle} />
              ))}
          </SimpleGrid>
        )}
      </ScrollArea>
      <ChallengeCreateModal
        title={t('admin.button.challenges.new')}
        size="30%"
        opened={createOpened}
        onClose={() => setCreateOpened(false)}
        onAddChallenge={(challenge) => mutate([challenge, ...(challenges ?? [])])}
      />
      <BloodBonusModel
        title={t('admin.button.challenges.bonus')}
        size="30%"
        opened={bonusOpened}
        onClose={() => setBonusOpened(false)}
      />
    </WithGameEditTab>
  )
}

export default GameChallengeEdit
