import {
    ActionIcon,
    Avatar,
    Button,
    Group,
    Paper,
    Select,
    Stack,
    Table,
    Text,
    Loader,
    ComboboxItem
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { mdiDelete, mdiAccountPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import api, { UserInfoModel } from '@Api'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'

export const Managers: FC = () => {
    const { id } = useParams()
    const gameId = parseInt(id ?? '0')
    const { t } = useTranslation()
    const modals = useModals()

    const [admins, setAdmins] = useState<UserInfoModel[]>()
    const [isLoadingAdmins, setIsLoadingAdmins] = useState(false)

    const [searchValue, setSearchValue] = useState('')
    const [debouncedSearch] = useDebouncedValue(searchValue, 300)
    const [selectedUser, setSelectedUser] = useState<string | null>(null)

    const [users, setUsers] = useState<UserInfoModel[]>()
    const [isLoadingUsers, setIsLoadingUsers] = useState(false)

    const fetchAdmins = async () => {
        if (!gameId) return
        setIsLoadingAdmins(true)
        try {
            const res = await api.edit.editGetGameAdmins(gameId)
            setAdmins((res.data as any).data || res.data)
        } catch (e) {
            showErrorMsg(e, t)
        } finally {
            setIsLoadingAdmins(false)
        }
    }

    useEffect(() => {
        fetchAdmins()
    }, [gameId])

    useEffect(() => {
        const fetchUsers = async () => {
            if (!debouncedSearch) {
                setUsers(undefined)
                return
            }
            setIsLoadingUsers(true)
            try {
                const res = await api.admin.adminGetUsers({ search: debouncedSearch, count: 10 })
                setUsers((res.data as any).data || res.data)
            } catch (e) {
                showErrorMsg(e, t)
            } finally {
                setIsLoadingUsers(false)
            }
        }
        fetchUsers()
    }, [debouncedSearch])

    const handleAddAdmin = async () => {
        if (!selectedUser || !gameId) return

        try {
            await api.edit.editAddGameAdmin(gameId, selectedUser)
            showSuccessMsg(t('admin.notification.games.managers.added'))
            setSelectedUser(null)
            setSearchValue('')
            fetchAdmins()
        } catch (e: any) {
            showErrorMsg(e, t)
        }
    }

    const handleRemoveAdmin = async (userId: string) => {
        if (!gameId) return

        try {
            await api.edit.editRemoveGameAdmin(gameId, userId)
            showSuccessMsg(t('admin.notification.games.managers.removed'))
            fetchAdmins()
        } catch (e: any) {
            showErrorMsg(e, t)
        }
    }

    const onConfirmRemove = (userId: string, userName: string | null | undefined) => {
        modals.openConfirmModal({
            title: t('admin.content.games.managers.delete_title'),
            children: (
                <Text size="sm">
                    {t('admin.content.games.managers.delete_confirm', { name: userName || 'this manager' })}
                </Text>
            ),
            onConfirm: () => handleRemoveAdmin(userId),
            confirmProps: { color: 'red' },
        })
    }

    const userOptions: ComboboxItem[] = (users ?? [])
        .filter((u): u is UserInfoModel & { id: string } => !!u.id)
        .map((u) => ({
            value: u.id,
            label: `${u.userName} (${u.email})`,
        }))

    return (
        <WithGameEditTab isLoading={isLoadingAdmins && !admins}>
            <Stack>
                <Paper withBorder p="md">
                    <Group align="flex-end">
                        <Select
                            label={t('admin.content.games.managers.select_user')}
                            placeholder={t('admin.content.games.managers.search_placeholder')}
                            data={userOptions}
                            value={selectedUser}
                            onChange={setSelectedUser}
                            searchValue={searchValue}
                            onSearchChange={setSearchValue}
                            searchable
                            clearable
                            nothingFoundMessage={
                                isLoadingUsers ? <Loader size="xs" /> : t('common.content.no_data')
                            }
                            w={400}
                            filter={({ options }) => options} // Server-side filtering
                        />
                        <Button
                            leftSection={<Icon path={mdiAccountPlus} size={1} />}
                            onClick={handleAddAdmin}
                            disabled={!selectedUser}
                        >
                            {t('common.button.add')}
                        </Button>
                    </Group>
                </Paper>

                <Paper withBorder p="0">
                    <Table>
                        <Table.Thead>
                            <Table.Tr>
                                <Table.Th>{t('common.label.user')}</Table.Th>
                                <Table.Th>{t('account.label.email')}</Table.Th>
                                <Table.Th w={100}>{t('common.label.action')}</Table.Th>
                            </Table.Tr>
                        </Table.Thead>
                        <Table.Tbody>
                            {isLoadingAdmins && (
                                <Table.Tr>
                                    <Table.Td colSpan={3}>
                                        <Group justify="center" p="md">
                                            <Loader />
                                        </Group>
                                    </Table.Td>
                                </Table.Tr>
                            )}
                            {admins?.map((admin) => (
                                <Table.Tr key={admin.id}>
                                    <Table.Td>
                                        <Group gap="xs">
                                            <Avatar src={admin.avatar} size="sm" radius="xl" />
                                            <Text size="sm" fw={500}>
                                                {admin.userName}
                                            </Text>
                                        </Group>
                                    </Table.Td>
                                    <Table.Td>{admin.email}</Table.Td>
                                    <Table.Td>
                                        <ActionIcon
                                            color="red"
                                            variant="subtle"
                                            onClick={() => admin.id && onConfirmRemove(admin.id, admin.userName)}
                                        >
                                            <Icon path={mdiDelete} size={1} />
                                        </ActionIcon>
                                    </Table.Td>
                                </Table.Tr>
                            ))}

                            {!isLoadingAdmins && admins?.length === 0 && (
                                <Table.Tr>
                                    <Table.Td colSpan={3} ta="center">
                                        <Text c="dimmed">{t('admin.content.games.managers.empty')}</Text>
                                    </Table.Td>
                                </Table.Tr>
                            )}
                        </Table.Tbody>
                    </Table>
                </Paper>
            </Stack>
        </WithGameEditTab>
    )
}

export default Managers
